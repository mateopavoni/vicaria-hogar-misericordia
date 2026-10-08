namespace Vicaria.Application.CalendarEvents;

public class PublishPersonalCalendarEventResult
{
    public bool IsSuccess => GeneralCalendarEventId.HasValue;
    public Guid? GeneralCalendarEventId { get; }
    public string? ErrorMessage { get; }

    private PublishPersonalCalendarEventResult(Guid? generalCalendarEventId, string? errorMessage)
    {
        GeneralCalendarEventId = generalCalendarEventId;
        ErrorMessage = errorMessage;
    }

    public static PublishPersonalCalendarEventResult Ok(Guid generalCalendarEventId) =>
        new(generalCalendarEventId, null);

    public static PublishPersonalCalendarEventResult NotFound(string message = "El evento especificado no existe.") =>
        new(null, message);
}