namespace Vicaria.Application.MedicationSchemes;

public enum ReviewMedicationSchemeError
{
    PersonNotFound,
    SchemeNotFound,
    AlreadyInactive
}

public class ReviewMedicationSchemeResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public ReviewMedicationSchemeError? Error { get; init; }

    public static ReviewMedicationSchemeResult Ok() => new()
    {
        Success = true
    };

    public static ReviewMedicationSchemeResult PersonNotFound() => new()
    {
        Success = false,
        Error = ReviewMedicationSchemeError.PersonNotFound,
        ErrorMessage = "La persona indicada no existe."
    };

    public static ReviewMedicationSchemeResult SchemeNotFound() => new()
    {
        Success = false,
        Error = ReviewMedicationSchemeError.SchemeNotFound,
        ErrorMessage = "El esquema de medicación no existe para esta persona."
    };

    public static ReviewMedicationSchemeResult AlreadyInactive() => new()
    {
        Success = false,
        Error = ReviewMedicationSchemeError.AlreadyInactive,
        ErrorMessage = "Solo se puede revisar un esquema vigente."
    };
}
