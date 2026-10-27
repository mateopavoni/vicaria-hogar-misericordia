namespace Vicaria.Domain.Entities;

// valor configurable del catálogo del informe Caritas (SCRUM-274):
// una sola tabla con discriminador Category; la baja es lógica
// (IsActive = false) para conservar los datos históricos al cambiar
// los criterios de Caritas
public class CaritasCatalogValue
{
    public Guid Id { get; set; }
    public CaritasCatalogCategory Category { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
