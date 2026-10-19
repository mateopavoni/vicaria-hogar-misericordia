namespace Vicaria.Domain.Entities;

public class Activity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsFromCalendar { get; set; }
    public DateTime CreatedAt { get; set; }
}