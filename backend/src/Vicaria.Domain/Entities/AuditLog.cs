namespace Vicaria.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string EntidadAfectada { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
}
