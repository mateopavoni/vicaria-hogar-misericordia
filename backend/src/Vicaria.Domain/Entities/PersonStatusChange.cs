namespace Vicaria.Domain.Entities;

// historial de cambios de estado de la ficha (SCRUM-262): cada transición
// Active<->Inactive queda registrada con fecha, motivo y actor; ChangedByUserId
// en null significa cambio automático del job (sin usuario). La crea esta
// subtarea y la reutiliza el cambio manual (SCRUM-263).
public class PersonStatusChange
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PersonId { get; set; }
    public Person Person { get; set; } = null!;
    public SocialRecordStatus PreviousStatus { get; set; }
    public SocialRecordStatus NewStatus { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid? ChangedByUserId { get; set; }
    public User? ChangedByUser { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
