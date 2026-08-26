namespace Vicaria.Domain.Entities;

public class Notification
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string? LinkUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? TargetRole { get; set; }
}
