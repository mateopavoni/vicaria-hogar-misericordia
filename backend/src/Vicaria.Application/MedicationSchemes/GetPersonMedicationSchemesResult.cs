namespace Vicaria.Application.MedicationSchemes;

public enum GetPersonMedicationSchemesError
{
    PersonNotFound
}

public class GetPersonMedicationSchemesResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public GetPersonMedicationSchemesError? Error { get; init; }
    public IReadOnlyList<MedicationSchemeDto>? Schemes { get; init; }

    public static GetPersonMedicationSchemesResult Ok(IReadOnlyList<MedicationSchemeDto> schemes) => new()
    {
        Success = true,
        Schemes = schemes
    };

    public static GetPersonMedicationSchemesResult PersonNotFound() => new()
    {
        Success = false,
        Error = GetPersonMedicationSchemesError.PersonNotFound,
        ErrorMessage = "La persona indicada no existe."
    };
}