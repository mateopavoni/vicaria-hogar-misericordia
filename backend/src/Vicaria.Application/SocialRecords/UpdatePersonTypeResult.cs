namespace Vicaria.Application.SocialRecords;

public enum UpdatePersonTypeError
{
    PersonNotFound,
    SocialRecordNotFound,
    MissingPsychiatricEvaluation,
    ActiveStayMustBeExitedFirst
}

public class UpdatePersonTypeResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public UpdatePersonTypeError? Error { get; init; }

    public static UpdatePersonTypeResult Ok() => new() { Success = true };

    public static UpdatePersonTypeResult PersonNotFound() =>
        new() { Success = false, Error = UpdatePersonTypeError.PersonNotFound, ErrorMessage = "La persona no existe." };

    public static UpdatePersonTypeResult SocialRecordNotFound() =>
        new() { Success = false, Error = UpdatePersonTypeError.SocialRecordNotFound, ErrorMessage = "La persona no tiene una ficha social asociada." };

    public static UpdatePersonTypeResult MissingPsychiatricEvaluation() =>
        new()
        {
            Success = false,
            Error = UpdatePersonTypeError.MissingPsychiatricEvaluation,
            ErrorMessage = "No se puede asignar el tipo Residente sin una evaluación psiquiátrica vigente cargada para la persona."
        };

    public static UpdatePersonTypeResult ActiveStayMustBeExitedFirst() =>
        new()
        {
            Success = false,
            Error = UpdatePersonTypeError.ActiveStayMustBeExitedFirst,
            ErrorMessage = "Debe registrar el egreso de la Casa de Convivencia antes de cambiar el tipo de persona."
        };
}