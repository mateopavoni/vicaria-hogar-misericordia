using Microsoft.EntityFrameworkCore;
using Vicaria.Application.PsychiatricEvaluations;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.Infrastructure.PsychiatricEvaluations;

public class PsychiatricEvaluationService : IPsychiatricEvaluationService
{
    // meses de vigencia de una evaluación desde su fecha (SCRUM-258); el vencimiento
    // se calcula, no se persiste, igual que el chequeo de MedicationSchemeService
    private const int ValidityMonths = 6;
    private static readonly TimeZoneInfo CordobaTimeZone = ResolveCordobaTimeZone();

    private readonly VicariaDbContext _dbContext;

    public PsychiatricEvaluationService(VicariaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public static DateTime GetCordobaToday() =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, CordobaTimeZone).Date;

    private static TimeZoneInfo ResolveCordobaTimeZone()
    {
        if (TimeZoneInfo.TryFindSystemTimeZoneById("America/Argentina/Cordoba", out var ianaTimeZone))
        {
            return ianaTimeZone;
        }

        if (TimeZoneInfo.TryFindSystemTimeZoneById("Argentina Standard Time", out var windowsTimeZone))
        {
            return windowsTimeZone;
        }

        return TimeZoneInfo.CreateCustomTimeZone(
            "America/Argentina/Cordoba",
            TimeSpan.FromHours(-3),
            "Hora de Córdoba",
            "Hora de Córdoba");
    }

    public async Task<CreatePsychiatricEvaluationResult> CreateAsync(
        Guid personId,
        CreatePsychiatricEvaluationDto dto,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var personExists = await _dbContext.People
            .AsNoTracking()
            .AnyAsync(p => p.Id == personId, cancellationToken);
        if (!personExists)
        {
            return CreatePsychiatricEvaluationResult.PersonNotFound();
        }

        var socialRecord = await _dbContext.SocialRecords
            .FirstOrDefaultAsync(r => r.PersonId == personId, cancellationToken);
        if (socialRecord is null)
        {
            return CreatePsychiatricEvaluationResult.SocialRecordNotFound();
        }

        // todo el alta es atómico: invalida anteriores, crea la vigente, actualiza la
        // ficha y audita. Dos SaveChanges para que los UPDATE corran antes del INSERT
        // (el índice único filtrado no admite dos vigentes ni siquiera transitorias)
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var previousValid = await _dbContext.PsychiatricEvaluations
                .Where(e => e.PersonId == personId && e.IsValid)
                .ToListAsync(cancellationToken);
            foreach (var previous in previousValid)
            {
                previous.IsValid = false;
            }
            await _dbContext.SaveChangesAsync(cancellationToken);

            var evaluation = new PsychiatricEvaluation
            {
                Id = Guid.NewGuid(),
                PersonId = personId,
                Date = dto.Date,
                Professional = dto.Professional,
                Diagnosis = dto.Diagnosis,
                IsValid = true,
                RegisteredByUserId = actorId
            };
            _dbContext.PsychiatricEvaluations.Add(evaluation);

            socialRecord.HasPsychiatricEvaluation = true;
            socialRecord.UpdatedAt = DateTime.UtcNow;

            _dbContext.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = actorId,
                Action = "Evaluación psiquiátrica registrada",
                AffectedEntity = $"PsychiatricEvaluation:{evaluation.Id}",
                Date = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return CreatePsychiatricEvaluationResult.Ok(new PsychiatricEvaluationDto(
                evaluation.Id,
                evaluation.PersonId,
                evaluation.Date,
                evaluation.Professional,
                evaluation.Diagnosis,
                evaluation.IsValid,
                evaluation.Date.AddMonths(ValidityMonths),
                evaluation.RegisteredByUserId));
        }
        catch (DbUpdateException)
        {
            // red de seguridad del índice único filtrado: otra alta concurrente dejó una
            // vigente primero; se revierte todo y se responde 409 para reintentar
            await transaction.RollbackAsync(cancellationToken);
            return CreatePsychiatricEvaluationResult.DuplicateValidEvaluation();
        }
    }

    public async Task<GetPsychiatricEvaluationStatusResult> GetStatusAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var personExists = await _dbContext.People
            .AsNoTracking()
            .AnyAsync(p => p.Id == personId, cancellationToken);

        if (!personExists)
        {
            return GetPsychiatricEvaluationStatusResult.PersonNotFound();
        }

        var evaluation = await _dbContext.PsychiatricEvaluations
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.PersonId == personId && e.IsValid, cancellationToken);

        if (evaluation is null)
        {
            return GetPsychiatricEvaluationStatusResult.Ok(new PsychiatricEvaluationStatusDto(
                HasEvaluation: false,
                EvaluationId: null,
                Date: null,
                ExpiresAt: null,
                DiasParaVencer: null,
                EstaVencida: true,
                AlertaVencimientoProximo: false,
                Professional: null,
                Diagnosis: null
            ));
        }

        var expiresAt = evaluation.Date.Date.AddMonths(ValidityMonths);
        var today = GetCordobaToday();
        var diasParaVencer = (int)(expiresAt - today).TotalDays;
        var estaVencida = diasParaVencer < 0;
        var alertaProxima = !estaVencida && diasParaVencer <= 30;

        var statusDto = new PsychiatricEvaluationStatusDto(
            HasEvaluation: true,
            EvaluationId: evaluation.Id,
            Date: evaluation.Date,
            ExpiresAt: expiresAt,
            DiasParaVencer: diasParaVencer,
            EstaVencida: estaVencida,
            AlertaVencimientoProximo: alertaProxima,
            Professional: evaluation.Professional,
            Diagnosis: evaluation.Diagnosis
        );

        return GetPsychiatricEvaluationStatusResult.Ok(statusDto);
    }

    public async Task<GetPsychiatricEvaluationHistoryResult> GetHistoryAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var personExists = await _dbContext.People
            .AsNoTracking()
            .AnyAsync(p => p.Id == personId, cancellationToken);

        if (!personExists)
        {
            return GetPsychiatricEvaluationHistoryResult.PersonNotFound();
        }

        var history = await _dbContext.PsychiatricEvaluations
            .AsNoTracking()
            .Include(e => e.RegisteredBy)
            .Where(e => e.PersonId == personId && !e.IsValid)
            .OrderByDescending(e => e.Date)
            .Select(e => new PsychiatricEvaluationHistoryItemDto(
                e.Id,
                e.PersonId,
                e.Date,
                e.Date.AddMonths(ValidityMonths),
                e.Professional,
                e.Diagnosis,
                e.IsValid,
                e.RegisteredByUserId,
                e.RegisteredBy != null ? $"{e.RegisteredBy.FirstName} {e.RegisteredBy.LastName}".Trim() : string.Empty
            ))
            .ToListAsync(cancellationToken);

        return GetPsychiatricEvaluationHistoryResult.Ok(history);
    }
}