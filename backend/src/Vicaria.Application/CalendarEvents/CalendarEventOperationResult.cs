namespace Vicaria.Application.CalendarEvents;

public enum CalendarEventOperationError
{
    NotFound,
    Forbidden
}

public class CalendarEventOperationResult
{
    public bool IsSuccess => Error is null;
    public CalendarEventOperationError? Error { get; }
    public string? ErrorMessage { get; }

    private CalendarEventOperationResult(CalendarEventOperationError? error, string? errorMessage)
    {
        Error = error;
        ErrorMessage = errorMessage;
    }

    public static CalendarEventOperationResult Ok() =>
        new(null, null);

    public static CalendarEventOperationResult NotFound(string message = "El evento especificado no existe.") =>
        new(CalendarEventOperationError.NotFound, message);

    public static CalendarEventOperationResult Forbidden(string message = "No tienes permiso para modificar o eliminar este evento.") =>
        new(CalendarEventOperationError.Forbidden, message);
}