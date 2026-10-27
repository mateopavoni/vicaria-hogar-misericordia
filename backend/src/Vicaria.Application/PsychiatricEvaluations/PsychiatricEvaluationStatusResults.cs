namespace Vicaria.Application.PsychiatricEvaluations;

public enum GetPsychiatricEvaluationStatusError
{
    PersonNotFound
}

public class GetPsychiatricEvaluationStatusResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public GetPsychiatricEvaluationStatusError? Error { get; init; }
    public PsychiatricEvaluationStatusDto? Data { get; init; }

    public static GetPsychiatricEvaluationStatusResult Ok(PsychiatricEvaluationStatusDto data) => new()
    {
        Success = true,
        Data = data
    };

    public static GetPsychiatricEvaluationStatusResult PersonNotFound() => new()
    {
        Success = false,
        Error = GetPsychiatricEvaluationStatusError.PersonNotFound,
        ErrorMessage = "La persona no existe."
    };
}

public enum GetPsychiatricEvaluationHistoryError
{
    PersonNotFound
}

public class GetPsychiatricEvaluationHistoryResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public GetPsychiatricEvaluationHistoryError? Error { get; init; }
    public IReadOnlyList<PsychiatricEvaluationHistoryItemDto>? History { get; init; }

    public static GetPsychiatricEvaluationHistoryResult Ok(IReadOnlyList<PsychiatricEvaluationHistoryItemDto> history) => new()
    {
        Success = true,
        History = history
    };

    public static GetPsychiatricEvaluationHistoryResult PersonNotFound() => new()
    {
        Success = false,
        Error = GetPsychiatricEvaluationHistoryError.PersonNotFound,
        ErrorMessage = "La persona no existe."
    };
}