using Vicaria.Domain.Entities;

namespace Vicaria.Application.CalendarEvents;

// alta de un evento del calendario general. Date va en UTC y la repetición
// se guarda en una sola fila con los días marcados en WeekDays.
public record CreateGeneralCalendarEventDto(
    string Title,
    DateTime Date,
    TimeSpan? StartTime = null,
    TimeSpan? EndTime = null,
    string? Description = null,
    WeekDays RecurrenceDays = WeekDays.None,
    bool RepeatsMonthly = false);
