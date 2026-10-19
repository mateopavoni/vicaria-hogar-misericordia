namespace Vicaria.Application.MedicationStocks;

public enum RegisterMedicationStockError
{
    InvalidData
}

public class RegisterMedicationStockResult
{
    public bool Success { get; init; }
    public Guid? MedicationId { get; init; }
    public RegisterMedicationStockError? Error { get; init; }
    public string? ErrorMessage { get; init; }

    public static RegisterMedicationStockResult Ok(Guid medicationId) =>
        new() { Success = true, MedicationId = medicationId };

    public static RegisterMedicationStockResult Invalid(string message) =>
        new() { Success = false, Error = RegisterMedicationStockError.InvalidData, ErrorMessage = message };
}

public enum GetMedicationStockHistoryError
{
    MedicationNotFound
}

public class GetMedicationStockHistoryResult
{
    public bool Success { get; init; }
    public MedicationStockHistoryDto? Data { get; init; }
    public GetMedicationStockHistoryError? Error { get; init; }
    public string? ErrorMessage { get; init; }

    public static GetMedicationStockHistoryResult Ok(MedicationStockHistoryDto data) =>
        new() { Success = true, Data = data };

    public static GetMedicationStockHistoryResult NotFound() =>
        new() { Success = false, Error = GetMedicationStockHistoryError.MedicationNotFound, ErrorMessage = "El medicamento no existe." };
}

public enum UpdateMedicationStockThresholdError
{
    MedicationNotFound
}

public class UpdateMedicationStockThresholdResult
{
    public bool Success { get; init; }
    public UpdateMedicationStockThresholdError? Error { get; init; }
    public string? ErrorMessage { get; init; }

    public static UpdateMedicationStockThresholdResult Ok() =>
        new() { Success = true };

    public static UpdateMedicationStockThresholdResult NotFound() =>
        new() { Success = false, Error = UpdateMedicationStockThresholdError.MedicationNotFound, ErrorMessage = "El medicamento no existe." };
}