namespace Vicaria.Application.Auth;

public enum RefreshTokenError
{
    InvalidRefreshToken,
    RefreshTokenExpired
}

public class RefreshTokenResult
{
    public bool Success { get; private init; }
    public string? ErrorMessage { get; private init; }
    public RefreshTokenError? Error { get; private init; }
    public string? Token { get; private init; }
    public string? RefreshToken { get; private init; }

    public static RefreshTokenResult Ok(string token, string refreshToken) =>
        new() { Success = true, Token = token, RefreshToken = refreshToken };

    public static RefreshTokenResult InvalidRefreshToken() =>
        new()
        {
            Success = false,
            Error = RefreshTokenError.InvalidRefreshToken,
            ErrorMessage = "El refresh token es inválido."
        };

    public static RefreshTokenResult RefreshTokenExpired() =>
        new()
        {
            Success = false,
            Error = RefreshTokenError.RefreshTokenExpired,
            ErrorMessage = "El refresh token venció, inicie sesión de nuevo."
        };
}
