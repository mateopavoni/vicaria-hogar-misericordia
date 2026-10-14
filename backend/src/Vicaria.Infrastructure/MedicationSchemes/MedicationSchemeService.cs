using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Vicaria.Application.MedicationSchemes;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.Infrastructure.MedicationSchemes;

public class MedicationSchemeService : IMedicationSchemeService
{
    private readonly VicariaDbContext _dbContext;

    public MedicationSchemeService(VicariaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CreateMedicationSchemeResult> CreateAsync(
        Guid personId,
        CreateMedicationSchemeDto dto,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        var personExists = await _dbContext.People
            .AsNoTracking()
            .AnyAsync(p => p.Id == personId, cancellationToken);
        if (!personExists)
        {
            return CreateMedicationSchemeResult.PersonNotFound();
        }

        // SCRUM-232 (decisión 11): solo Residentes
        var isResident = await _dbContext.SocialRecords
            .AsNoTracking()
            .AnyAsync(r => r.PersonId == personId && r.PersonType == PersonType.Resident, cancellationToken);
        if (!isResident)
        {
            return CreateMedicationSchemeResult.PersonNotResident();
        }

        // SCRUM-232 (decisión 14): sin evaluación psiquiátrica vigente se bloquea la carga
        var hasValidEvaluation = await _dbContext.PsychiatricEvaluations
            .AsNoTracking()
            .AnyAsync(e => e.PersonId == personId && e.IsValid, cancellationToken);
        if (!hasValidEvaluation)
        {
            return CreateMedicationSchemeResult.MissingPsychiatricEvaluation();
        }

        var medication = await _dbContext.MedicationCatalogs
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == dto.MedicationId, cancellationToken);
        if (medication is null)
        {
            return CreateMedicationSchemeResult.MedicationNotFound();
        }

        // el validador ya garantiza el formato HH:mm
        var time = TimeSpan.ParseExact(dto.Time, @"hh\:mm", CultureInfo.InvariantCulture);

        // decisión 10: la misma persona + medicamento + hora no se repite mientras
        // siga vigente (los históricos con IsActive = false no bloquean una recarga)
        var duplicate = await _dbContext.MedicationSchemes
            .AsNoTracking()
            .AnyAsync(s =>
                s.PersonId == personId &&
                s.MedicationId == dto.MedicationId &&
                s.Time == time &&
                s.IsActive, cancellationToken);
        if (duplicate)
        {
            return CreateMedicationSchemeResult.DuplicateScheme();
        }

        var actor = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == actorId, cancellationToken);

        var scheme = new MedicationScheme
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            MedicationId = medication.Id,
            Dose = dto.Dose.Trim(),
            Time = time,
            LoadedAt = DateTime.UtcNow,
            LoadedByUserId = actorId,
            IsActive = true,
            PendingStock = medication.Stock <= 0
        };
        _dbContext.MedicationSchemes.Add(scheme);

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = "Esquema de medicación creado",
            AffectedEntity = $"MedicationScheme:{scheme.Id}",
            Date = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreateMedicationSchemeResult.Ok(new MedicationSchemeDto(
            scheme.Id,
            scheme.PersonId,
            scheme.MedicationId,
            medication.Name,
            scheme.Dose,
            scheme.Time.ToString(@"hh\:mm"),
            scheme.LoadedAt,
            scheme.LoadedByUserId,
            actor is null ? string.Empty : $"{actor.FirstName} {actor.LastName}".Trim(),
            scheme.IsActive,
            scheme.PendingStock));
    }
}
