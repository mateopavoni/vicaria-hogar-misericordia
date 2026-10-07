namespace Vicaria.Application.CalendarEvents;

public interface IGeneralCalendarEventService
{
    Task<CreateGeneralCalendarEventResult> CreateAsync(
        CreateGeneralCalendarEventDto dto,
        Guid actorId,
        CancellationToken cancellationToken);
}
