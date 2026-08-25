namespace Vicaria.Application.Auth;

public enum LoginError
{
    UserNotFound,
    InvalidCredentials,
    InvalidState
}

public class LoginResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public LoginError? Error { get; init; }
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }

    public static LoginResult Ok(string accessToken, string refreshToken) =>
        new() { Success = true, AccessToken = accessToken, RefreshToken = refreshToken };

    public static LoginResult UserNotFound() =>
        new() { Success = false, Error = LoginError.UserNotFound, ErrorMessage = "El usuario no existe." };

    public static LoginResult InvalidCredentials() =>
        new() { Success = false, Error = LoginError.InvalidCredentials, ErrorMessage = "Las credenciales son incorrectas." };

    public static LoginResult InvalidState() =>
        new() { Success = false, Error = LoginError.InvalidState, ErrorMessage = "La cuenta no está activa." };
}
