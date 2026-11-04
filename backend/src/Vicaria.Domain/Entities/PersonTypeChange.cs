namespace Vicaria.Domain.Entities;

// historial de cambios de tipo de persona: se registra cada vez que PersonType cambia,
// sin importar desde qué endpoint
public class PersonTypeChange
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PersonId { get; set; }
    public Person Person { get; set; } = null!;
    public PersonType? PreviousType { get; set; }
    public PersonType NewType { get; set; }
    public Guid ChangedByUserId { get; set; }
    public User ChangedByUser { get; set; } = null!;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
