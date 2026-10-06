namespace Vicaria.Domain.Entities;

public class Observation
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public Person Person { get; set; } = null!;
    public string Content { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public ObservationCategory? Category { get; set; }
    public Guid AuthorUserId { get; set; }
    public User AuthorUser { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}