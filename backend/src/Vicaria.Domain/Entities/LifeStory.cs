namespace Vicaria.Domain.Entities;

public class LifeStory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PersonId { get; set; }
    public Person Person { get; set; } = null!;

    public string? BeforeCentroBarrial { get; set; }
    public Guid? BeforeCentroBarrialUpdatedByUserId { get; set; }
    public User? BeforeCentroBarrialUpdatedByUser { get; set; }
    public DateTime? BeforeCentroBarrialUpdatedAt { get; set; }

    public string? InCentroBarrial { get; set; }
    public Guid? InCentroBarrialUpdatedByUserId { get; set; }
    public User? InCentroBarrialUpdatedByUser { get; set; }
    public DateTime? InCentroBarrialUpdatedAt { get; set; }
    public string? AfterCentroBarrial { get; set; }
    public Guid? AfterCentroBarrialUpdatedByUserId { get; set; }
    public User? AfterCentroBarrialUpdatedByUser { get; set; }
    public DateTime? AfterCentroBarrialUpdatedAt { get; set; }
}