namespace Vicaria.Application.MedicationSchemes;

public enum DeactivateMedicationSchemeError
{
    PersonNotFound,
    SchemeNotFound,
    AlreadyInactive
}

public class DeactivateMedicationSchemeResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public DeactivateMedicationSchemeError? Error { get; init; }

    public static DeactivateMedicationSchemeResult Ok() => new()
    {
        Success = true
    };

    public static DeactivateMedicationSchemeResult PersonNotFound() => new()
    {
        Success = false,
        Error = DeactivateMedicationSchemeError.PersonNotFound,
        ErrorMessage = "La persona indicada no existe."
    };

    public static DeactivateMedicationSchemeResult SchemeNotFound() => new()
    {
        Success = false,
        Error = DeactivateMedicationSchemeError.SchemeNotFound,
        ErrorMessage = "El esquema de medicación no existe para esta persona."
    };

    public static DeactivateMedicationSchemeResult AlreadyInactive() => new()
    {
        Success = false,
        Error = DeactivateMedicationSchemeError.AlreadyInactive,
        ErrorMessage = "El esquema de medicación ya se encuentra inactivo."
    };
}