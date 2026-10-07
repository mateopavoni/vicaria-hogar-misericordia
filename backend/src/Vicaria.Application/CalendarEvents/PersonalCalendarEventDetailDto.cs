using Vicaria.Domain.Entities;

namespace Vicaria.Application.CalendarEvents;

// detalle sin expandir de un evento personal (SCRUM-194); el autor es siempre
// el usuario autenticado que lo consulta, porque los ajenos ni se materializan
public record PersonalCalendarEventDetailDto(
    Guid Id,
    string Title,
    string? Description,
    DateTime? Date,
    TimeSpan? StartTime,
    TimeSpan? EndTime,
    WeekDays RecurrenceDays,
    Guid AuthorUserId,
    DateTime CreatedAt);
