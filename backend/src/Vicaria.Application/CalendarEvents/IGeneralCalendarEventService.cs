using Vicaria.Application.Common;

namespace Vicaria.Application.CalendarEvents;

public interface IGeneralCalendarEventService
{
    Task<CreateGeneralCalendarEventResult> CreateAsync(
        CreateGeneralCalendarEventDto dto,
        Guid actorId,
        CancellationToken cancellationToken);

    Task<PagedResult<CalendarEventOccurrenceDto>> GetOccurrencesAsync(
        DateTime from,
        DateTime to,
        int page,
        CancellationToken cancellationToken);

    Task<GeneralCalendarEventDetailDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);
}
