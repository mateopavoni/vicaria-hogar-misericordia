namespace Vicaria.Domain.Entities;

public class LifeStory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PersonId { get; set; }
    public Person Person { get; set; } = null!;

    public string? BeforeHogar { get; set; }
    public Guid? BeforeHogarUpdatedByUserId { get; set; }
    public User? BeforeHogarUpdatedByUser { get; set; }
    public DateTime? BeforeHogarUpdatedAt { get; set; }

    public string? InHogar { get; set; }
    public Guid? InHogarUpdatedByUserId { get; set; }
    public User? InHogarUpdatedByUser { get; set; }
    public DateTime? InHogarUpdatedAt { get; set; }
    public string? AfterHogar { get; set; }
    public Guid? AfterHogarUpdatedByUserId { get; set; }
    public User? AfterHogarUpdatedByUser { get; set; }
    public DateTime? AfterHogarUpdatedAt { get; set; }
}