using Vicaria.Domain.Entities;

namespace Vicaria.Application.CalendarEvents;

// detalle sin expandir de un evento del calendario general (SCRUM-194);
// AuthorUserId es nullable porque las plantillas del seed no tienen autor
public record GeneralCalendarEventDetailDto(
    Guid Id,
    string Title,
    string? Description,
    DateTime? Date,
    TimeSpan? StartTime,
    TimeSpan? EndTime,
    WeekDays RecurrenceDays,
    Guid? AuthorUserId,
    string? AuthorName,
    DateTime CreatedAt,
    bool RepeatsMonthly = false);
