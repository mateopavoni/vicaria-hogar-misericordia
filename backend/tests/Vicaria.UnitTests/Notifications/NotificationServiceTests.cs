using Microsoft.EntityFrameworkCore;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Notifications;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.Notifications;

public class NotificationServiceTests
{
    private static VicariaDbContext CrearDbContext()
    {
        var options = new DbContextOptionsBuilder<VicariaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new VicariaDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static async Task<Guid> CrearNotificacion(VicariaDbContext db, string role, bool isRead = false)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            Description = "Nueva notificación",
            EventType = "NuevoUsuarioPendiente",
            IsRead = isRead,
            CreatedAt = DateTime.UtcNow,
            TargetRole = role
        };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();
        return notification.Id;
    }

    [Fact]
    public async Task MarkAllAsReadAsync_MarcaTodasyRegistraAuditLogPorNotificacion()
    {
        using var db = CrearDbContext();
        var role = RoleNames.Referente;
        var primera = await CrearNotificacion(db, role);
        var segunda = await CrearNotificacion(db, role);
        var actorId = Guid.NewGuid();
        var service = new NotificationService(db);

        await service.MarkAllAsReadAsync(role, actorId);

        var notifications = await db.Notifications.Where(n => n.TargetRole == role).ToListAsync();
        Assert.All(notifications, n => Assert.True(n.IsRead));

        var logs = await db.AuditLogs.ToListAsync();
        Assert.Equal(2, logs.Count);
        Assert.Contains(logs, l => l.AffectedEntity == $"Notificacion:{primera}" && l.UserId == actorId);
        Assert.Contains(logs, l => l.AffectedEntity == $"Notificacion:{segunda}" && l.UserId == actorId);
        Assert.All(logs, l => Assert.Equal("MarcarNotificacionLeida", l.Action));
    }

    [Fact]
    public async Task MarkAllAsReadAsync_SinNotificacionesPendientes_NoRegistraAuditLog()
    {
        using var db = CrearDbContext();
        var role = RoleNames.Referente;
        await CrearNotificacion(db, role, isRead: true);
        var service = new NotificationService(db);

        await service.MarkAllAsReadAsync(role, Guid.NewGuid());

        Assert.Empty(await db.AuditLogs.ToListAsync());
    }
}