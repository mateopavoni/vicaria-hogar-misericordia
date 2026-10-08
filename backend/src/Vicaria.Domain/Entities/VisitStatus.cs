namespace Vicaria.Domain.Entities;

// estados de una visita a la Casa de Convivencia (SCRUM-209):
// Pendiente / Realizada / Cancelada del Jira
public enum VisitStatus
{
    Pending,
    Completed,
    Cancelled
}
