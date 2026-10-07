namespace Vicaria.Domain.Entities;

// eventos del calendario general (agenda compartida del hogar, EP-04/SCRUM-183).
// Date null + RecurrenceDays con valores = plantilla recurrente precargada por seed;
// Date con valor = evento concreto cargado en una fecha. AuthorUserId anulado porque
// las plantillas institucionales no tienen autor (seed entra con null).
public class GeneralCalendarEvent
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? Date { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public WeekDays RecurrenceDays { get; set; }
    public Guid? AuthorUserId { get; set; }
    public User? AuthorUser { get; set; }
    public DateTime CreatedAt { get; set; }
}
