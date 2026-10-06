using Vicaria.Domain.Entities;

namespace Vicaria.Application.Events;

public record EventOccurrenceDto(
    Guid Id,
    string Title,
    string? Description,
    DateTime StartDate,
    DateTime EndDate,
    bool IsRecurring,
    Guid AuthorUserId,
    string AuthorName
);

public record EventDetailDto(
    Guid Id,
    string Title,
    string? Description,
    DateTime StartDate,
    DateTime EndDate,
    bool IsRecurring,
    EventRecurrencePattern RecurrencePattern,
    DayOfWeek[]? RecurrenceDays,
    DateTime? RecurrenceUntil,
    Guid AuthorUserId,
    string AuthorName,
    DateTime CreatedAt
);