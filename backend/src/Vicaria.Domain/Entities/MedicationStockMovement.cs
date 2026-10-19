namespace Vicaria.Domain.Entities;

// registro de movimientos de inventario de medicamentos (SCRUM-244 / historia SCRUM-71):
// ingresos con proveedor y vencimiento, y consumos automáticos o manuales.
public class MedicationStockMovement
{
    public Guid Id { get; set; }
    public Guid MedicationId { get; set; }
    public MedicationCatalog? Medication { get; set; }
    public MedicationStockMovementType Type { get; set; }
    public int Quantity { get; set; }
    public DateTime Date { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string? Supplier { get; set; }
    public Guid CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
}