namespace Vicaria.Application.Notifications;

public interface INotificationService
{
    Task<IReadOnlyList<NotificationDto>> GetByRoleAsync(string role, CancellationToken cancellationToken = default);
    Task<MarkAsReadResult> MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default);
}
