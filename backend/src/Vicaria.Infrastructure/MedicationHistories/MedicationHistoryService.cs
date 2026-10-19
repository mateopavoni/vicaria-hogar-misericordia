using Microsoft.EntityFrameworkCore;
using Vicaria.Application.MedicationHistories;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.MedicationSchemes;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.Infrastructure.MedicationHistories;

public class MedicationHistoryService : IMedicationHistoryService
{
    private readonly VicariaDbContext _dbContext;

    public MedicationHistoryService(VicariaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MedicationHistoryResult> GetPersonHistoryAsync(Guid personId, GetMedicationHistoryDto filter, IEnumerable<string> actorRoles, CancellationToken cancellationToken = default)
    {
        var personExists = await _dbContext.People
            .AsNoTracking()
            .AnyAsync(p => p.Id == personId, cancellationToken);
        if (!personExists)
        {
            return MedicationHistoryResult.PersonNotFound();
        }

        // Directora y Coordinadora solo ven a Residentes con estadía abierta en Casa de Convivencia
        // (criterio Y acordado). El tipo Residente lo valida el filtro [DirectorResidentsOnly].
        var roles = actorRoles.ToList();
        var isRestricted = !roles.Contains(RoleNames.Referent) &&
                           (roles.Contains(RoleNames.CasaConvivenciaDirector) || roles.Contains(RoleNames.CasaConvivenciaCoordinator));
        if (isRestricted)
        {
            var hasOpenStay = await _dbContext.CasaConvivenciaStays
                .AsNoTracking()
                .AnyAsync(s => s.PersonId == personId && s.ExitDate == null, cancellationToken);
            if (!hasOpenStay)
            {
                return MedicationHistoryResult.PersonNotFound();
            }
        }

        // "hoy" en hora de Córdoba: las tomas de hoy todavía no tienen resultado histórico
        var today = MedicationSchemeService.GetCordobaToday();

        var query = _dbContext.DailyMedicationSchedules
            .AsNoTracking()
            .Include(d => d.MedicationScheme)
            .Include(d => d.Medication)
            .Include(d => d.AdministeredByUser)
            .Where(d => d.PersonId == personId && d.Date < today);

        if (filter.From.HasValue)
        {
            var from = filter.From.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(d => d.Date >= from);
        }

        if (filter.To.HasValue)
        {
            var to = filter.To.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(d => d.Date <= to);
        }

        if (filter.MedicationId.HasValue)
        {
            query = query.Where(d => d.MedicationId == filter.MedicationId.Value);
        }

        var rows = await query
            .OrderBy(d => d.Date)
            .ThenBy(d => d.Time)
            .ToListAsync(cancellationToken);

        var entries = rows
            // esquemas desactivados: solo se muestran las tomas que sí se administraron
            .Where(d => (d.MedicationScheme != null && d.MedicationScheme.IsActive) || d.Status == MedicationIntakeStatus.Administered)
            .Select(d => new MedicationHistoryEntryDto(
                Date: DateOnly.FromDateTime(d.Date),
                Time: d.Time.ToString(@"hh\:mm"),
                SchemeId: d.MedicationSchemeId,
                MedicationId: d.MedicationId,
                MedicationName: d.Medication?.Name ?? string.Empty,
                Dose: d.Dose,
                Status: d.Status switch
                {
                    MedicationIntakeStatus.Administered => "administered",
                    MedicationIntakeStatus.NotAdministered => "not-administered",
                    _ => "unregistered"
                },
                Observation: d.Observation,
                RecordedByUserId: d.AdministeredByUserId,
                RecordedByName: d.AdministeredByUser is null
                    ? null
                    : $"{d.AdministeredByUser.FirstName} {d.AdministeredByUser.LastName}".Trim(),
                RecordedAt: d.AdministeredAt
            ))
            .ToList();

        return MedicationHistoryResult.Ok(entries);
    }
}
