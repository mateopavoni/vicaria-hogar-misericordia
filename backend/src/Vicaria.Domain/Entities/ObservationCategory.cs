namespace Vicaria.Domain.Entities;

// categoría para clasificar observaciones (SCRUM-176): nombre + descripción opcional.
// La desactivación es un soft delete (IsActive), nunca borrado físico.
public class ObservationCategory
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}