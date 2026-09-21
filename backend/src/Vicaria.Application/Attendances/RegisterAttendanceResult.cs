namespace Vicaria.Application.Attendances;

public enum RegisterAttendanceError
{
    PersonNotFound,
    SocialRecordNotFound
}

public class RegisterAttendanceResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public RegisterAttendanceError? Error { get; init; }

    public static RegisterAttendanceResult Ok() => new() { Success = true };

    public static RegisterAttendanceResult PersonNotFound() =>
        new() { Success = false, Error = RegisterAttendanceError.PersonNotFound, ErrorMessage = "La persona no existe." };

    public static RegisterAttendanceResult SocialRecordNotFound() =>
        new() { Success = false, Error = RegisterAttendanceError.SocialRecordNotFound, ErrorMessage = "La persona no tiene una ficha social asociada." };
}