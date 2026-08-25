namespace Vicaria.Application.Auth;

public enum RefreshTokenError
{
    InvalidRefreshToken,
    RefreshTokenExpired
}

public class RefreshTokenResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public RefreshTokenError? Error { get; init; }
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }

    public static RefreshTokenResult Ok(string accessToken, string refreshToken) =>
        new() { Success = true, AccessToken = accessToken, RefreshToken = refreshToken };

    public static RefreshTokenResult InvalidRefreshToken() =>
        new() { Success = false, Error = RefreshTokenError.InvalidRefreshToken, ErrorMessage = "El refresh token es inválido." };

    public static RefreshTokenResult RefreshTokenExpired() =>
        new() { Success = false, Error = RefreshTokenError.RefreshTokenExpired, ErrorMessage = "El refresh token ha expirado." };
}
