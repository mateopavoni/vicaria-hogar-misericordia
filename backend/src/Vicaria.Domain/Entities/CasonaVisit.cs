namespace Vicaria.Domain.Entities;

// visita de un residente a la Casa de Convivencia (SCRUM-209 / SCRUM-74): entidad
// independiente de los eventos del calendario (GeneralCalendarEvent/PersonalCalendarEvent).
// VisitorName es texto libre porque no existe tabla de visitantes; CancellationReason
// solo aplica cuando Status = Cancelled.
public class CasonaVisit
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public Person? Person { get; set; }
    public string VisitorName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public VisitStatus Status { get; set; } = VisitStatus.Pending;
    public string? CancellationReason { get; set; }
    public Guid CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
