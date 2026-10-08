using Vicaria.Application.Common;

namespace Vicaria.Application.CalendarEvents;

// calendario personal: todas las consultas quedan atadas al actor tomado del JWT,
// los eventos de otro usuario no son consultables por nadie más, sin excepción de rol
public interface IPersonalCalendarEventService
{
    // alta de evento propio: mismo cuerpo que el general; el autor es siempre el actor
    Task<Guid> CreateAsync(
        CreateGeneralCalendarEventDto dto,
        Guid actorId,
        CancellationToken cancellationToken);

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
}
