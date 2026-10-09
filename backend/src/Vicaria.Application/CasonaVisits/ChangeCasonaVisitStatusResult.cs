namespace Vicaria.Application.CasonaVisits;

public enum ChangeCasonaVisitStatusError
{
    NotFound,
    InvalidState
}

public class ChangeCasonaVisitStatusResult
{
    public bool IsSuccess => Error is null;
    public ChangeCasonaVisitStatusError? Error { get; }
    public string? ErrorMessage { get; }

    private ChangeCasonaVisitStatusResult(ChangeCasonaVisitStatusError? error, string? errorMessage)
    {
        Error = error;
        ErrorMessage = errorMessage;
    }

    public static ChangeCasonaVisitStatusResult Ok() =>
        new(null, null);

    public static ChangeCasonaVisitStatusResult NotFound(string message = "La visita especificada no existe.") =>
        new(ChangeCasonaVisitStatusError.NotFound, message);

    public static ChangeCasonaVisitStatusResult InvalidState(string message = "No se puede cambiar el estado de la visita.") =>
        new(ChangeCasonaVisitStatusError.InvalidState, message);
}