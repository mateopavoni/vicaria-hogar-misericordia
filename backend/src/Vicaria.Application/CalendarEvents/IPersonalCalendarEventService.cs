using Vicaria.Application.Common;

namespace Vicaria.Application.CalendarEvents;

// calendario personal: todas las consultas quedan atadas al actor tomado del JWT,
// los eventos de otro usuario no son consultables por nadie más, sin excepción de rol
public interface IPersonalCalendarEventService
{
    Task<PagedResult<CalendarEventOccurrenceDto>> GetOccurrencesAsync(
        DateTime from,
        DateTime to,
        int page,
        Guid actorId,
        CancellationToken cancellationToken);

    Task<PersonalCalendarEventDetailDto?> GetByIdAsync(
        Guid id,
        Guid actorId,
        CancellationToken cancellationToken);

        Task<PublishPersonalCalendarEventResult> PublishAsync(
        Guid id,
        Guid actorId,
        CancellationToken cancellationToken);
}
