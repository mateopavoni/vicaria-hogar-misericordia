using Vicaria.Domain.Entities;

namespace Vicaria.Application.MedicationStocks;

public record RegisterMedicationStockEntryDto(
    string Nombre,
    int Cantidad,
    DateTime FechaVencimiento,
    string? Proveedor
);

public record MedicationStockMovementItemDto(
    Guid Id,
    Guid MedicationId,
    string MedicationName,
    MedicationStockMovementType Type,
    string TypeDescription,
    int Quantity,
    DateTime Date,
    DateTime? ExpirationDate,
    string? Supplier,
    Guid CreatedByUserId
);

public record MedicationStockHistoryDto(
    Guid MedicationId,
    string MedicationName,
    int CurrentStock,
    IReadOnlyList<MedicationStockMovementItemDto> Movements
);

public record MedicationStockAlertDto(
    Guid MedicationId,
    string MedicationName,
    int CurrentStock,
    int MinimumStockThreshold,
    bool IsOutOfStock
);

public record UpdateMedicationStockThresholdDto(
    int MinimumStockThreshold
);