namespace Vicaria.Application.CasonaVisits;

// alta de visita de un residente a la Casa de Convivencia (SCRUM-210 / SCRUM-74).
// PersonId es el residente que recibe la visita; VisitorName es texto libre del visitante
// (no existe tabla de visitantes, ver CasonaVisit). El alta siempre queda en Pending
// (default de la entidad); el estado se cambia después por PUT.
public record CreateCasonaVisitDto(
    Guid PersonId,
    string VisitorName,
    DateTime Date,
    TimeSpan StartTime,
    int EstimatedDurationMinutes,
    // el solapamiento es una advertencia: sin esto responde 409 y el cliente puede reenviar
    // con true si el usuario decide guardar igual
    bool AllowOverlap = false);
