using Vicaria.Domain.Entities;

namespace Vicaria.Application.CalendarEvents;

public record UpdateGeneralCalendarEventDto(
    string Title,
    string? Description,
    DateTime? Date,
    TimeSpan? StartTime,
    TimeSpan? EndTime,
    WeekDays RecurrenceDays,
    bool RepeatsMonthly = false);