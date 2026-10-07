namespace Vicaria.Domain.Entities;

// calendario personal por usuario (EP-04/SCRUM-183); a diferencia del general,
// el autor es obligatorio: cada evento personal pertenece a quien lo crea.
public class PersonalCalendarEvent
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? Date { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public WeekDays RecurrenceDays { get; set; }
    public Guid AuthorUserId { get; set; }
    public User AuthorUser { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
