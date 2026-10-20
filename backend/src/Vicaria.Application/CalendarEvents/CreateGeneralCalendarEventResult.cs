namespace Vicaria.Application.CalendarEvents;

public class CreateGeneralCalendarEventResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid GeneralCalendarEventId { get; init; }

    public static CreateGeneralCalendarEventResult Ok(Guid generalCalendarEventId) => new()
    {
        Success = true,
        GeneralCalendarEventId = generalCalendarEventId
    };
}
