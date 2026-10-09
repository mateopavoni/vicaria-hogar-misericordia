using Vicaria.Domain.Entities;

namespace Vicaria.Application.CalendarEvents;

// una ocurrencia concreta de un evento en el calendario (SCRUM-194): los eventos
// recurrentes se expanden día por día dentro del rango consultado
public record CalendarEventOccurrenceDto(
    Guid EventId,
    DateTime Date,
    TimeSpan? StartTime,
    TimeSpan? EndTime,
    string Title,
    string? Description,
    Guid? AuthorUserId = null,
    string? AuthorName = null,
    // plantillas precargadas del sistema (Date null): el calendario no las deja editar
    bool IsPreloaded = false,
    // regla de repetición del evento, para que el cliente pueda editarlo sin perderla
    WeekDays RecurrenceDays = WeekDays.None,
    bool RepeatsMonthly = false);
