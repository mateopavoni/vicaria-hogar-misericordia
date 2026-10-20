namespace Vicaria.Application.Notifications;

public record NotificationDto(Guid Id, string Description, string EventType, string? LinkUrl, bool IsRead, DateTime CreatedAt);
