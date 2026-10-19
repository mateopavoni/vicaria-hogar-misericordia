namespace Vicaria.Application.Attendances;

// cuerpo de PUT /api/attendances/{id} (SCRUM-217): corrección de una asistencia
// existente; AnonymousCount es opcional (null = no cambia) para no resetear el
// conteo cuando el frontend solo envía la justificación
public record UpdateAttendanceDto(int? AnonymousCount = null, string? Justification = null);
