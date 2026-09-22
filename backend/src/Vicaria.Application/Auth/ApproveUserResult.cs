namespace Vicaria.Application.Auth;

public enum ApproveUserError
{
    UserNotFound,
    InvalidState,
    InvalidRole,
    CannotActOnSelf
}

public class ApproveUserResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public ApproveUserError? Error { get; init; }

    public static ApproveUserResult Ok() => new() { Success = true };

    public static ApproveUserResult UserNotFound() =>
        new() { Success = false, Error = ApproveUserError.UserNotFound, ErrorMessage = "El usuario no existe." };

    public static ApproveUserResult InvalidState() =>
        new() { Success = false, Error = ApproveUserError.InvalidState, ErrorMessage = "El usuario no está pendiente de aprobación." };

    public static ApproveUserResult InvalidRole() =>
        new() { Success = false, Error = ApproveUserError.InvalidRole, ErrorMessage = "El rol indicado no existe." };

    public static ApproveUserResult CannotActOnSelf() =>
        new() { Success = false, Error = ApproveUserError.CannotActOnSelf, ErrorMessage = "No podés realizar esta acción sobre tu propia cuenta." };
}
