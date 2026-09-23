namespace Vicaria.Domain.Entities;

// registro diario de asistencia de una persona (SCRUM-135): se usa para mantener
// Activo el estado de la ficha mientras la persona asiste cada 30 días; si no hay
// asistencia reciente, un job programado pasa la ficha a Inactive
public class Attendance
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public Person? Person { get; set; }
    public DateTime Date { get; set; }
    public Guid CreatedByUserId { get; set; }
}