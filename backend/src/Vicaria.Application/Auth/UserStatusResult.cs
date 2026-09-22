namespace Vicaria.Application.Auth;

public enum UserStatusError
{
    UserNotFound,
    AlreadyInThatState,
    CannotActOnSelf
}

public class UserStatusResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public UserStatusError? Error { get; init; }

    public static UserStatusResult Ok() => new() { Success = true };

    public static UserStatusResult UserNotFound() =>
        new() { Success = false, Error = UserStatusError.UserNotFound, ErrorMessage = "El usuario no existe." };

    public static UserStatusResult AlreadyInThatState(string message) =>
        new() { Success = false, Error = UserStatusError.AlreadyInThatState, ErrorMessage = message };

    public static UserStatusResult CannotActOnSelf(string message) =>
        new() { Success = false, Error = UserStatusError.CannotActOnSelf, ErrorMessage = message };
}
