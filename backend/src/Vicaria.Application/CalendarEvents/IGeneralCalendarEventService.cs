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

        Task<CalendarEventOperationResult> UpdateAsync(
        Guid id,
        UpdateGeneralCalendarEventDto dto,
        Guid actorId,
        bool isReferent,
        CancellationToken cancellationToken);

    Task<CalendarEventOperationResult> DeleteAsync(
        Guid id,
        Guid actorId,
        bool isReferent,
        CancellationToken cancellationToken);
}
