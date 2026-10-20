using Vicaria.Domain.Entities;

namespace Vicaria.Application.CalendarEvents;

// alta de evento del calendario general (SCRUM-189/EP-04).
// Date en UTC; recurrencia con los flags WeekDays del modelo SCRUM-183 (fila unica con la regla).
public record CreateGeneralCalendarEventDto(
    string Title,
    DateTime Date,
    TimeSpan? StartTime = null,
    TimeSpan? EndTime = null,
    string? Description = null,
    WeekDays RecurrenceDays = WeekDays.None,
    bool RepeatsMonthly = false);
