namespace Vicaria.Application.Attendances;

// cuerpo de POST /api/attendance (SCRUM-135): registra la asistencia diaria de una persona
public record CreateAttendanceDto(Guid PersonId);