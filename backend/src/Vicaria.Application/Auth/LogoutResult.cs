namespace Vicaria.Application.Auth;

public class LogoutResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }

    public static LogoutResult Ok() => new() { Success = true };

    public static LogoutResult Error(string message) =>
        new() { Success = false, ErrorMessage = message };
}
