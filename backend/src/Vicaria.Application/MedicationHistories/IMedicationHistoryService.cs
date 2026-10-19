namespace Vicaria.Application.MedicationHistories;

public interface IMedicationHistoryService
{
    Task<MedicationHistoryResult> GetPersonHistoryAsync(Guid personId, GetMedicationHistoryDto filter, IEnumerable<string> actorRoles, CancellationToken cancellationToken = default);
}
