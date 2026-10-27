namespace Vicaria.Application.PsychiatricEvaluations;

public enum CreatePsychiatricEvaluationError
{
    PersonNotFound,
    SocialRecordNotFound,
    DuplicateValidEvaluation
}

public class CreatePsychiatricEvaluationResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public CreatePsychiatricEvaluationError? Error { get; init; }
    public PsychiatricEvaluationDto? Evaluation { get; init; }

    public static CreatePsychiatricEvaluationResult Ok(PsychiatricEvaluationDto evaluation) => new()
    {
        Success = true,
        Evaluation = evaluation
    };

    public static CreatePsychiatricEvaluationResult PersonNotFound() =>
        new()
        {
            Success = false,
            Error = CreatePsychiatricEvaluationError.PersonNotFound,
            ErrorMessage = "La persona no existe."
        };

    public static CreatePsychiatricEvaluationResult SocialRecordNotFound() =>
        new()
        {
            Success = false,
            Error = CreatePsychiatricEvaluationError.SocialRecordNotFound,
            ErrorMessage = "La persona no tiene una ficha social asociada."
        };

    public static CreatePsychiatricEvaluationResult DuplicateValidEvaluation() =>
        new()
        {
            Success = false,
            Error = CreatePsychiatricEvaluationError.DuplicateValidEvaluation,
            ErrorMessage = "Ya se registró otra evaluación vigente para la persona. Volvé a intentarlo."
        };
}
