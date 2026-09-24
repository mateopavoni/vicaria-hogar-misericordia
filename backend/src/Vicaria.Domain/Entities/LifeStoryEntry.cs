namespace Vicaria.Domain.Entities;

// bitácora append-only de la historia de vida (bug reportado 2026-09-23: guardar una etapa
// pisaba el contenido anterior en vez de sumar una entrada nueva). Cada guardado de una etapa
// crea una fila nueva acá; LifeStory sigue existiendo como caché de "última entrada por etapa".
public class LifeStoryEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PersonId { get; set; }
    public Person Person { get; set; } = null!;
    public LifeStoryStage Stage { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
