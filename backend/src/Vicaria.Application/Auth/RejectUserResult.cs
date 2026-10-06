namespace Vicaria.Application.Auth;

public enum RejectUserError
{
    UserNotFound,
    InvalidState,
    CannotActOnSelf
}

public class RejectUserResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public RejectUserError? Error { get; init; }

    public static RejectUserResult Ok() => new() { Success = true };

    public static RejectUserResult UserNotFound() =>
        new() { Success = false, Error = RejectUserError.UserNotFound, ErrorMessage = "El usuario no existe." };

    public static RejectUserResult InvalidState() =>
        new() { Success = false, Error = RejectUserError.InvalidState, ErrorMessage = "El usuario no está pendiente de aprobación." };

    public static RejectUserResult CannotActOnSelf() =>
        new() { Success = false, Error = RejectUserError.CannotActOnSelf, ErrorMessage = "No podés realizar esta acción sobre tu propia cuenta." };
}
