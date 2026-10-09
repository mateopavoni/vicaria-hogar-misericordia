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

    // edición y baja de un evento propio; el de otro usuario se trata como inexistente (404)
    Task<CalendarEventOperationResult> UpdateAsync(
        Guid id,
        UpdateGeneralCalendarEventDto dto,
        Guid actorId,
        CancellationToken cancellationToken);

    Task<CalendarEventOperationResult> DeleteAsync(
        Guid id,
        Guid actorId,
        CancellationToken cancellationToken);

    Task<PublishPersonalCalendarEventResult> PublishAsync(
        Guid id,
        Guid actorId,
        CancellationToken cancellationToken);
}
