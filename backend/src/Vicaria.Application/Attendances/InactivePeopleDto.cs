namespace Vicaria.Application.Attendances;

// lista de personas sin asistencia reciente (SCRUM-225): criterio puro por
// asistencia (última fecha registrada), independiente del job de 30 días que
// cambia SocialRecord.Status a Inactive
public record InactivePersonDto(
    Guid PersonId,
    string FirstName,
    string? LastName,
    DateTime? LastAttendanceDate,
    int? DaysSinceLastAttendance
);

public record GetInactivePeopleQueryDto(int Days = 14);

public class GetInactivePeopleResult
{
    public bool Success { get; init; }
    public IReadOnlyList<InactivePersonDto>? Data { get; init; }
    public string? ErrorMessage { get; init; }

    public static GetInactivePeopleResult Ok(IReadOnlyList<InactivePersonDto> data) =>
        new() { Success = true, Data = data };
}
