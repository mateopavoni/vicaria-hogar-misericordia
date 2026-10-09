namespace Vicaria.Domain.Entities;

// colaborador (SCRUM-199 / EP-05): voluntario o empleado del dispositivo, no es una
// persona atendida ni un usuario del sistema. Dni/Email/Phone/WorkArea son opcionales;
// el índice único sobre Dni solo aplica cuando está cargado (ver CollaboratorConfiguration).
public class Collaborator
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string? Dni { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public CollaboratorType Type { get; set; }
    public string? WorkArea { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid RegisteredByUserId { get; set; }
    public User? RegisteredByUser { get; set; }
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}
