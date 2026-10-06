namespace Vicaria.Domain.Entities;

// historial de cambios de tipo de persona (bug reportado 2026-09-23: el historial nunca existió
// en el backend). Se registra cada vez que PersonType cambia de verdad, sin importar por cuál de
// los 3 endpoints que pueden tocarlo (UpdatePersonType, UpdateAsync de ficha, UpdatePersonProfileStatus).
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
