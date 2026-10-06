namespace Vicaria.Domain.Entities;

public class Event
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsRecurring { get; set; }
    public EventRecurrencePattern RecurrencePattern { get; set; } = EventRecurrencePattern.None;
    public DayOfWeek[]? RecurrenceDays { get; set; }
    public DateTime? RecurrenceUntil { get; set; }

    public Guid AuthorUserId { get; set; }
    public User AuthorUser { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}