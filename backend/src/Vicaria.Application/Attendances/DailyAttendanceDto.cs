using Vicaria.Domain.Entities;

namespace Vicaria.Application.Attendances;

// asistencia de una actividad en una fecha: personas con ficha y total de personas
// sin ficha (suma del AnonymousCount de los registros del día)
public record DailyAttendancePersonDto(Guid AttendanceId, Guid PersonId, string FullName, string? Dni, int AnonymousCount);

public record DailyAttendanceDto(
    AttendanceActivity Activity,
    DateTime Date,
    int AnonymousCount,
    IReadOnlyList<DailyAttendancePersonDto> Persons
);
