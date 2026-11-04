namespace Vicaria.Domain.Entities;

// catálogo de medicamentos disponibles: solo medicamentos
// del catálogo pueden cargarse en un esquema. Stock = unidades en farmacia;
// 0 = sin stock (el esquema se marca como pendiente).
// MinimumStockThreshold = umbral configurable para alertas de stock bajo.
public class MedicationCatalog
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Stock { get; set; }
    public int MinimumStockThreshold { get; set; } = 5;
}