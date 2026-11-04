using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Vicaria.Application.Common;
using Vicaria.Application.MedicationSchemes;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.Infrastructure.MedicationSchemes;

public class MedicationSchemeService : IMedicationSchemeService
{
    // Un esquema vigente sin revisar más de estos días dispara la alerta
    private const int ReviewAlertDays = 30;
    private readonly VicariaDbContext _dbContext;

    public MedicationSchemeService(VicariaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CreateMedicationSchemeResult> CreateAsync(
        Guid personId,
        CreateMedicationSchemeDto dto,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var personExists = await _dbContext.People
            .AsNoTracking()
            .AnyAsync(p => p.Id == personId, cancellationToken);
        if (!personExists)
        {
            return CreateMedicationSchemeResult.PersonNotFound();
        }

        // (decisión 11): solo Residentes
        var isResident = await _dbContext.SocialRecords
            .AsNoTracking()
            .AnyAsync(r => r.PersonId == personId && r.PersonType == PersonType.Resident, cancellationToken);
        if (!isResident)
        {
            return CreateMedicationSchemeResult.PersonNotResident();
        }

        // Sin evaluación psiquiátrica vigente (IsValid y no vencida a más de 6 meses en fecha de Córdoba) se bloquea la carga
        var minValidEvaluationDate = CordobaClock.Today.AddMonths(-6);
        var hasValidEvaluation = await _dbContext.PsychiatricEvaluations
            .AsNoTracking()
            .AnyAsync(e =>
                e.PersonId == personId &&
                e.IsValid &&
                e.Date.Date >= minValidEvaluationDate, cancellationToken);
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
            scheme.PendingStock,
            scheme.ReviewedAt,
            scheme.ReviewedByUserId,
            null,
            scheme.PendingStock ? MedicationStockAlertDto.PendingStock() : null));
    }

   public async Task<GetPersonMedicationSchemesResult> GetByPersonIdAsync(Guid personId, bool? onlyActive = null, bool? needsReview = null, CancellationToken cancellationToken = default)
    {
        var personExists = await _dbContext.People
            .AsNoTracking()
            .AnyAsync(p => p.Id == personId, cancellationToken);

        if (!personExists)
        {
            return GetPersonMedicationSchemesResult.PersonNotFound();
        }

        var query = _dbContext.MedicationSchemes
            .AsNoTracking()
            .Include(s => s.Medication)
            .Include(s => s.LoadedBy)
            .Include(s => s.ReviewedBy)
            .Where(s => s.PersonId == personId);

        if (onlyActive.HasValue)
        {
            var activeFilter = onlyActive.Value;
            query = query.Where(s => s.IsActive == activeFilter);
        }

        // Solo los vigentes que llevan más de 30 días sin revisión
        if (needsReview == true)
        {
            var threshold = CordobaClock.Now.AddDays(-ReviewAlertDays);
            query = query.Where(s => s.IsActive && (s.ReviewedAt ?? s.LoadedAt) < threshold);
        }

        var schemes = await query
            .OrderByDescending(s => s.IsActive)
            .ThenByDescending(s => s.LoadedAt)
            .Select(s => new MedicationSchemeDto(
                s.Id,
                s.PersonId,
                s.MedicationId,
                s.Medication != null ? s.Medication.Name : string.Empty,
                s.Dose,
                s.Time.ToString(@"hh\:mm"),
                s.LoadedAt,
                s.LoadedByUserId,
                s.LoadedBy != null ? $"{s.LoadedBy.FirstName} {s.LoadedBy.LastName}".Trim() : string.Empty,
                s.IsActive,
                s.PendingStock,
                s.ReviewedAt,
                s.ReviewedByUserId,
                s.ReviewedBy != null ? $"{s.ReviewedBy.FirstName} {s.ReviewedBy.LastName}".Trim() : null,
                null
            ))
            .ToListAsync(cancellationToken);

        return GetPersonMedicationSchemesResult.Ok(schemes);
    }

    public async Task<IReadOnlyList<MedicationCatalogItemDto>> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.MedicationCatalogs
            .AsNoTracking()
            .OrderBy(m => m.Name)
            .Select(m => new MedicationCatalogItemDto(m.Id, m.Name, m.Stock))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MedicationReviewAlertDto>> GetNeedsReviewAsync(CancellationToken cancellationToken = default)
    {
        var threshold = CordobaClock.Now.AddDays(-ReviewAlertDays);

        var schemes = await _dbContext.MedicationSchemes
            .AsNoTracking()
            .Include(s => s.Medication)
            .Include(s => s.Person)
            .Where(s => s.IsActive && (s.ReviewedAt ?? s.LoadedAt) < threshold)
            .OrderBy(s => s.ReviewedAt ?? s.LoadedAt)
            .ToListAsync(cancellationToken);

        var now = CordobaClock.Now;
        return schemes.Select(s => new MedicationReviewAlertDto(
            s.Id,
            s.PersonId,
            s.Person != null ? $"{s.Person.FirstName} {s.Person.LastName}".Trim() : string.Empty,
            s.MedicationId,
            s.Medication != null ? s.Medication.Name : string.Empty,
            s.Dose,
            s.Time.ToString(@"hh\:mm"),
            s.ReviewedAt,
            s.LoadedAt,
            (int)(now - (s.ReviewedAt ?? s.LoadedAt)).TotalDays
        )).ToList();
    }

    public async Task<ReviewMedicationSchemeResult> ReviewAsync(Guid personId, Guid schemeId, Guid actorId, CancellationToken cancellationToken = default)
    {
        var personExists = await _dbContext.People
            .AsNoTracking()
            .AnyAsync(p => p.Id == personId, cancellationToken);

        if (!personExists)
        {
            return ReviewMedicationSchemeResult.PersonNotFound();
        }

        var scheme = await _dbContext.MedicationSchemes
            .FirstOrDefaultAsync(s => s.Id == schemeId && s.PersonId == personId, cancellationToken);

        if (scheme is null)
        {
            return ReviewMedicationSchemeResult.SchemeNotFound();
        }

        if (!scheme.IsActive)
        {
            return ReviewMedicationSchemeResult.AlreadyInactive();
        }

        scheme.ReviewedAt = DateTime.UtcNow;
        scheme.ReviewedByUserId = actorId;

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = "Esquema de medicación revisado",
            AffectedEntity = $"MedicationScheme:{scheme.Id}",
            Date = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ReviewMedicationSchemeResult.Ok();
    }

    public async Task<DeactivateMedicationSchemeResult> DeactivateAsync(Guid personId, Guid schemeId, Guid actorId, CancellationToken cancellationToken = default)
    {
        var personExists = await _dbContext.People
            .AsNoTracking()
            .AnyAsync(p => p.Id == personId, cancellationToken);

        if (!personExists)
        {
            return DeactivateMedicationSchemeResult.PersonNotFound();
        }

        var scheme = await _dbContext.MedicationSchemes
            .FirstOrDefaultAsync(s => s.Id == schemeId && s.PersonId == personId, cancellationToken);

        if (scheme is null)
        {
            return DeactivateMedicationSchemeResult.SchemeNotFound();
        }

        if (!scheme.IsActive)
        {
            return DeactivateMedicationSchemeResult.AlreadyInactive();
        }

        scheme.IsActive = false;

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = "Esquema de medicación desactivado",
            AffectedEntity = $"MedicationScheme:{scheme.Id}",
            Date = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return DeactivateMedicationSchemeResult.Ok();
    }
}