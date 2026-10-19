using Microsoft.EntityFrameworkCore;
using Vicaria.Application.MedicationAgendas;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.Infrastructure.MedicationAgendas;

public class MedicationAgendaService : IMedicationAgendaService
{
    private readonly VicariaDbContext _dbContext;

    public MedicationAgendaService(VicariaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DailyMedicationAgendaDto> GetDailyAgendaAsync(DateTime? date, CancellationToken cancellationToken = default)
    {
        var targetDate = (date ?? DateTime.UtcNow).Date;

        var intakes = await _dbContext.DailyMedicationSchedules
            .AsNoTracking()
            .Include(d => d.Person)
            .Include(d => d.Medication)
            .Where(d => d.Date == targetDate)
            .OrderBy(d => d.Time)
            .ThenBy(d => d.Person != null ? d.Person.LastName : string.Empty)
            .ThenBy(d => d.Person != null ? d.Person.FirstName : string.Empty)
            .ToListAsync(cancellationToken);

        var groups = intakes
            .GroupBy(d => d.Time)
            .Select(g => new MedicationAgendaGroupDto(
                Time: g.Key.ToString(@"hh\:mm"),
                Intakes: g.Select(d =>
                {
                    var fullName = d.Person != null
                        ? (!string.IsNullOrWhiteSpace(d.Person.LastName)
                            ? $"{d.Person.FirstName} {d.Person.LastName}".Trim()
                            : d.Person.FirstName)
                        : string.Empty;

                    return new MedicationAgendaItemDto(
                        Id: d.Id,
                        PersonId: d.PersonId,
                        PersonFullName: fullName,
                        MedicationId: d.MedicationId,
                        MedicationName: d.Medication?.Name ?? string.Empty,
                        Dose: d.Dose,
                        Status: d.Status,
                        Observation: d.Observation,
                        AdministeredAt: d.AdministeredAt,
                        AdministeredByUserId: d.AdministeredByUserId
                    );
                }).ToList()
            ))
            .ToList();

        return new DailyMedicationAgendaDto(targetDate, groups);
    }

    public async Task<UpdateMedicationIntakeResult> UpdateStatusAsync(Guid intakeId, UpdateMedicationIntakeStatusDto dto, Guid actorId, CancellationToken cancellationToken = default)
    {
        if (dto.Status != MedicationIntakeStatus.Administered && dto.Status != MedicationIntakeStatus.NotAdministered)
        {
            return UpdateMedicationIntakeResult.InvalidStatus("El estado debe ser Administrada o No administrada.");
        }

        var intake = await _dbContext.DailyMedicationSchedules
            .FirstOrDefaultAsync(d => d.Id == intakeId, cancellationToken);

        if (intake is null)
        {
            return UpdateMedicationIntakeResult.NotFound();
        }

        intake.Status = dto.Status;
        intake.Observation = string.IsNullOrWhiteSpace(dto.Observation) ? null : dto.Observation.Trim();
        intake.AdministeredAt = DateTime.UtcNow;
        intake.AdministeredByUserId = actorId;

        // Descuento automático de stock al marcar como Administrada (SCRUM-245)
        if (dto.Status == MedicationIntakeStatus.Administered)
        {
            var medication = await _dbContext.MedicationCatalogs
                .FirstOrDefaultAsync(m => m.Id == intake.MedicationId, cancellationToken);

            if (medication != null)
            {
                medication.Stock = Math.Max(0, medication.Stock - 1);

                _dbContext.MedicationStockMovements.Add(new MedicationStockMovement
                {
                    Id = Guid.NewGuid(),
                    MedicationId = medication.Id,
                    Type = MedicationStockMovementType.Consumo,
                    Quantity = 1,
                    Date = DateTime.UtcNow,
                    CreatedByUserId = actorId
                });
            }
        }

        var statusText = dto.Status == MedicationIntakeStatus.Administered ? "Administrada" : "No administrada";

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = $"Toma de medicación marcada como {statusText}",
            AffectedEntity = $"DailyMedicationSchedule:{intake.Id}",
            Date = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return UpdateMedicationIntakeResult.Ok();
    }
}