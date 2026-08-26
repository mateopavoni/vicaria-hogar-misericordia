namespace Vicaria.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string AffectedEntity { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}
