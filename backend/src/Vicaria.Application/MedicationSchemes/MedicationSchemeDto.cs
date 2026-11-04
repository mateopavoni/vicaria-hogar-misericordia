namespace Vicaria.Application.MedicationSchemes;

public record MedicationStockAlertDto(
    string Code,
    string Message)
{
    public const string PendingStockCode = "PendingStock";
    public const string PendingStockMessage = "Sin stock en farmacia: el medicamento queda marcado como pendiente de stock.";

    public static MedicationStockAlertDto PendingStock() =>
        new(PendingStockCode, PendingStockMessage);
}

// forma de respuesta del esquema creado: el front usa
// el objeto devuelto directamente, por eso viaja completo y no solo el id.
// Time en HH:mm y LoadedAt en ISO, igual que MedicationScheme del front.
// ReviewedAt/ReviewedBy* son la última revisión mensual, null si nunca se revisó.
public record MedicationSchemeDto(
    Guid Id,
    Guid PersonId,
    Guid MedicationId,
    string MedicationName,
    string Dose,
    string Time,
    DateTime LoadedAt,
    Guid LoadedByUserId,
    string LoadedByName,
    bool IsActive,
    bool PendingStock,
    DateTime? ReviewedAt,
    Guid? ReviewedByUserId,
    string? ReviewedByName,
    MedicationStockAlertDto? StockAlert = null);
