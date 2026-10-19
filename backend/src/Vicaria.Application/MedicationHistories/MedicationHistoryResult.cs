namespace Vicaria.Application.MedicationHistories;

public enum MedicationHistoryError
{
    PersonNotFound
}

public class MedicationHistoryResult
{
    public bool Success { get; init; }
    public MedicationHistoryError? Error { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<MedicationHistoryEntryDto>? Entries { get; init; }

    public static MedicationHistoryResult Ok(IReadOnlyList<MedicationHistoryEntryDto> entries) => new()
    {
        Success = true,
        Entries = entries
    };

    // también se usa cuando una Directora o Coordinadora consulta a una persona que no es
    // Residente con estadía abierta: mismo mensaje para no revelar que la persona existe
    public static MedicationHistoryResult PersonNotFound() => new()
    {
        Success = false,
        Error = MedicationHistoryError.PersonNotFound,
        ErrorMessage = "La persona indicada no existe."
    };
}
