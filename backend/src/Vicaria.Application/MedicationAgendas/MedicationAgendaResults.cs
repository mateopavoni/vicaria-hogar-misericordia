namespace Vicaria.Application.MedicationAgendas;

public enum UpdateMedicationIntakeError
{
    NotFound,
    InvalidStatus
}

public class UpdateMedicationIntakeResult
{
    public bool Success { get; init; }
    public UpdateMedicationIntakeError? Error { get; init; }
    public string? ErrorMessage { get; init; }

    public static UpdateMedicationIntakeResult Ok() => new() { Success = true };

    public static UpdateMedicationIntakeResult NotFound() =>
        new() { Success = false, Error = UpdateMedicationIntakeError.NotFound, ErrorMessage = "La toma de medicación no existe." };

    public static UpdateMedicationIntakeResult InvalidStatus(string message) =>
        new() { Success = false, Error = UpdateMedicationIntakeError.InvalidStatus, ErrorMessage = message };
}