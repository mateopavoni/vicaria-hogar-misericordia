namespace Vicaria.Domain.Entities;

// estadía de una persona en la Casa de Convivencia (SCRUM-140): FK 1:N a Person, una persona
// puede tener múltiples estadías. Solo EntryDate es obligatorio (fichas flexibles).
// SCRUM-146: ExitReason enum + Reason (texto libre solo para Other).
public class CasaConvivenciaStay
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public Person? Person { get; set; }
    public DateTime EntryDate { get; set; }
    public DateTime? ExitDate { get; set; }
    public StayExitReason? ExitReason { get; set; }
    public string? Reason { get; set; }
}
