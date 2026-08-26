namespace Vicaria.Application.Notifications;

public class MarkAsReadResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }

    public static MarkAsReadResult Ok() => new() { Success = true };

    public static MarkAsReadResult NotFound() =>
        new() { Success = false, ErrorMessage = "La notificación no existe." };
}
