namespace Vicaria.Domain.Entities;

// historial de la historia de vida: cada guardado de una etapa crea una fila nueva.
// LifeStory guarda solo la última entrada de cada etapa.
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
