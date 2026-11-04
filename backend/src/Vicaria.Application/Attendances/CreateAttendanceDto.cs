using Vicaria.Domain.Entities;

namespace Vicaria.Application.Attendances;

// cuerpo de POST /api/attendances: registra la asistencia diaria de una persona
public record CreateAttendanceDto(Guid PersonId, AttendanceActivity Activity, DateTime Date, int AnonymousCount = 0);