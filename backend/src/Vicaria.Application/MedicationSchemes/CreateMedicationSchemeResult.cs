namespace Vicaria.Application.MedicationSchemes;

public enum CreateMedicationSchemeError
{
    PersonNotFound,
    PersonNotResident,
    MissingPsychiatricEvaluation,
    MedicationNotFound,
    DuplicateScheme
}

public class CreateMedicationSchemeResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public CreateMedicationSchemeError? Error { get; init; }
    public MedicationSchemeDto? Scheme { get; init; }

    public static CreateMedicationSchemeResult Ok(MedicationSchemeDto scheme) => new()
    {
        Success = true,
        Scheme = scheme
    };

    public static CreateMedicationSchemeResult PersonNotFound() =>
        new()
        {
            Success = false,
            Error = CreateMedicationSchemeError.PersonNotFound,
            ErrorMessage = "La persona indicada no existe."
        };

    public static CreateMedicationSchemeResult PersonNotResident() =>
        new()
        {
            Success = false,
            Error = CreateMedicationSchemeError.PersonNotResident,
            ErrorMessage = "Solo se puede cargar medicación para personas con tipo Residente."
        };

    public static CreateMedicationSchemeResult MissingPsychiatricEvaluation() =>
        new()
        {
            Success = false,
            Error = CreateMedicationSchemeError.MissingPsychiatricEvaluation,
            ErrorMessage = "No se puede cargar medicación: la persona no tiene una evaluación psiquiátrica vigente."
        };

    public static CreateMedicationSchemeResult MedicationNotFound() =>
        new()
        {
            Success = false,
            Error = CreateMedicationSchemeError.MedicationNotFound,
            ErrorMessage = "El medicamento no existe en el catálogo."
        };

    public static CreateMedicationSchemeResult DuplicateScheme() =>
        new()
        {
            Success = false,
            Error = CreateMedicationSchemeError.DuplicateScheme,
            ErrorMessage = "Ya existe un esquema vigente con ese medicamento y horario para la persona."
        };
}
