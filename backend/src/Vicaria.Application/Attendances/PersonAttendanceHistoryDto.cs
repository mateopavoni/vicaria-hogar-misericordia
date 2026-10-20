using Vicaria.Domain.Entities;

namespace Vicaria.Application.Attendances;

public record PersonAttendanceIndicatorsDto(
    int TotalDiasAsistidos,
    int RachaActual,
    DateTime? UltimaAsistencia,
    bool SinAsistir7Dias
);

public record PersonAttendanceItemDto(
    Guid Id,
    DateTime Date,
    int AnonymousCount
);

public record ActivityAttendanceGroupDto(
    AttendanceActivity Activity,
    string ActivityName,
    IReadOnlyList<PersonAttendanceItemDto> Attendances
);

public record PersonAttendanceHistoryDto(
    Guid PersonId,
    PersonAttendanceIndicatorsDto Indicators,
    IReadOnlyList<ActivityAttendanceGroupDto> Groups
);

public record GetPersonAttendanceHistoryFilterDto(
    AttendanceActivity? Activity,
    DateTime? FromDate,
    DateTime? ToDate
);

public enum GetPersonAttendanceHistoryError
{
    NotFound,
    Forbidden
}

public record GetPersonAttendanceHistoryResult(
    PersonAttendanceHistoryDto? Data = null,
    GetPersonAttendanceHistoryError? Error = null,
    string? ErrorMessage = null
)
{
    public static GetPersonAttendanceHistoryResult Success(PersonAttendanceHistoryDto data) =>
        new(Data: data);

    public static GetPersonAttendanceHistoryResult NotFound(string message = "La persona especificada no existe.") =>
        new(Error: GetPersonAttendanceHistoryError.NotFound, ErrorMessage: message);

    public static GetPersonAttendanceHistoryResult Forbidden(string message = "No tiene autorización para ver el historial de esta persona.") =>
        new(Error: GetPersonAttendanceHistoryError.Forbidden, ErrorMessage: message);
}