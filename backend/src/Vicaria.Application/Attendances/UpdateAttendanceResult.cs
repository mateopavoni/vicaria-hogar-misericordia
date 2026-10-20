namespace Vicaria.Application.Attendances;

public enum UpdateAttendanceError
{
    NotFound,
    JustificationRequired
}

public class UpdateAttendanceResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public UpdateAttendanceError? Error { get; init; }

    public static UpdateAttendanceResult Ok() => new() { Success = true };

    public static UpdateAttendanceResult NotFound() =>
        new() { Success = false, Error = UpdateAttendanceError.NotFound, ErrorMessage = "El registro de asistencia no existe." };

    public static UpdateAttendanceResult JustificationRequired() =>
        new() { Success = false, Error = UpdateAttendanceError.JustificationRequired, ErrorMessage = "La justificación es obligatoria para modificar un día cerrado." };
}
