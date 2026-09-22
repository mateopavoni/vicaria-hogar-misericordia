namespace Vicaria.Application.Persons;

public enum UpdatePersonProfileStatusError
{
    PersonNotFound,
    SocialRecordNotFound,
    MissingPsychiatricEvaluation
}

public class UpdatePersonProfileStatusResult
{
    public bool Success => Error is null;
    public UpdatePersonProfileStatusError? Error { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static UpdatePersonProfileStatusResult Ok() => new();

    public static UpdatePersonProfileStatusResult PersonNotFound() => new()
    {
        Error = UpdatePersonProfileStatusError.PersonNotFound,
        ErrorMessage = "La persona no fue encontrada."
    };

    public static UpdatePersonProfileStatusResult SocialRecordNotFound() => new()
    {
        Error = UpdatePersonProfileStatusError.SocialRecordNotFound,
        ErrorMessage = "La ficha social de la persona no fue encontrada."
    };

    public static UpdatePersonProfileStatusResult MissingPsychiatricEvaluation() => new()
    {
        Error = UpdatePersonProfileStatusError.MissingPsychiatricEvaluation,
        ErrorMessage = "No se puede asignar el estado de Residente sin una evaluación psiquiátrica vigente."
    };
}