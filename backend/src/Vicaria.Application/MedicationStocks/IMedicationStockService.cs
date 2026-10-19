namespace Vicaria.Application.MedicationStocks;

public interface IMedicationStockService
{
    Task<RegisterMedicationStockResult> RegisterStockEntryAsync(RegisterMedicationStockEntryDto dto, Guid actorId, CancellationToken cancellationToken = default);
    Task<GetMedicationStockHistoryResult> GetMovementsByMedicationAsync(Guid medicationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MedicationStockAlertDto>> GetStockAlertsAsync(CancellationToken cancellationToken = default);
    Task<UpdateMedicationStockThresholdResult> UpdateThresholdAsync(Guid medicationId, UpdateMedicationStockThresholdDto dto, Guid actorId, CancellationToken cancellationToken = default);
}