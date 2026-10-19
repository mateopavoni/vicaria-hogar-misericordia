namespace Vicaria.Application.MedicationAgendas;

public interface IMedicationAgendaService
{
    Task<DailyMedicationAgendaDto> GetDailyAgendaAsync(DateTime? date, CancellationToken cancellationToken = default);
    Task<UpdateMedicationIntakeResult> UpdateStatusAsync(Guid intakeId, UpdateMedicationIntakeStatusDto dto, Guid actorId, CancellationToken cancellationToken = default);
}