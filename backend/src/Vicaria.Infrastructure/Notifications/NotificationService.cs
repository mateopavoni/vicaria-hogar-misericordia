using Microsoft.EntityFrameworkCore;
using Vicaria.Application.Notifications;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.Infrastructure.Notifications;

public class NotificationService : INotificationService
{
    private readonly VicariaDbContext _dbContext;

    public NotificationService(VicariaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<NotificationDto>> GetByRoleAsync(string role, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notifications
            .Where(n => n.TargetRole == role)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto(n.Id, n.Description, n.EventType, n.LinkUrl, n.IsRead, n.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<MarkAsReadResult> MarkAsReadAsync(Guid id, Guid actorId, CancellationToken cancellationToken = default)
    {
        var notification = await _dbContext.Notifications.FindAsync([id], cancellationToken);
        if (notification is null)
        {
            return MarkAsReadResult.NotFound();
        }

        notification.IsRead = true;

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = "MarcarNotificacionLeida",
            AffectedEntity = $"Notificacion:{id}",
            Date = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MarkAsReadResult.Ok();
    }

    public async Task MarkAllAsReadAsync(string role, CancellationToken cancellationToken = default)
    {
        var notifications = await _dbContext.Notifications
            .Where(n => n.TargetRole == role && !n.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
