namespace Vicaria.Application.SocialRecords;

public enum UpdateSocialRecordError
{
    NotFound,
    MissingPsychiatricEvaluation,
    ActiveStayMustBeExitedFirst
}

public class UpdateSocialRecordResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public UpdateSocialRecordError? Error { get; init; }

    public static UpdateSocialRecordResult Ok() => new() { Success = true };

    public static UpdateSocialRecordResult NotFound() =>
        new() { Success = false, Error = UpdateSocialRecordError.NotFound, ErrorMessage = "La ficha no existe." };

    public static UpdateSocialRecordResult MissingPsychiatricEvaluation() =>
        new()
        {
            Success = false,
            Error = UpdateSocialRecordError.MissingPsychiatricEvaluation,
            ErrorMessage = "No se puede asignar el tipo Residente sin una evaluación psiquiátrica vigente cargada para la persona."
        };

    public static UpdateSocialRecordResult ActiveStayMustBeExitedFirst() =>
        new()
        {
            Success = false,
            Error = UpdateSocialRecordError.ActiveStayMustBeExitedFirst,
            ErrorMessage = "Debe registrar el egreso de la Casa de Convivencia antes de cambiar el tipo de persona."
        };
}
