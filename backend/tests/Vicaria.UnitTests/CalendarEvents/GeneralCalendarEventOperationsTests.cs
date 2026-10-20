using Microsoft.EntityFrameworkCore;
using Vicaria.Application.CalendarEvents;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.CalendarEvents;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.CalendarEvents;

public class GeneralCalendarEventOperationsTests
{
    private static VicariaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VicariaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new VicariaDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static async Task<Guid> SeedUserAsync(VicariaDbContext db, string name = "Actor")
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = name,
            LastName = "Test",
            Email = $"{Guid.NewGuid()}@mail.com",
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    private static async Task<GeneralCalendarEvent> SeedEventAsync(
        VicariaDbContext db,
        Guid? authorId,
        string title = "Evento de prueba")
    {
        var ev = new GeneralCalendarEvent
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = "Descripción original",
            Date = DateTime.UtcNow.Date.AddDays(2),
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(12, 0, 0),
            AuthorUserId = authorId,
            CreatedAt = DateTime.UtcNow
        };
        db.GeneralCalendarEvents.Add(ev);
        await db.SaveChangesAsync();
        return ev;
    }

    private static UpdateGeneralCalendarEventDto NewUpdateDto(string title = "Título actualizado") =>
        new(title, "Descripción actualizada", DateTime.UtcNow.Date.AddDays(3), new TimeSpan(14, 0, 0), new TimeSpan(16, 0, 0), WeekDays.None);

    [Fact]
    public async Task UpdateAsync_WhenAuthorUpdates_SucceedsAndLogsAudit()
    {
        using var db = CreateDbContext();
        var authorId = await SeedUserAsync(db, "Author");
        var ev = await SeedEventAsync(db, authorId);
        var service = new GeneralCalendarEventService(db);

        var result = await service.UpdateAsync(ev.Id, NewUpdateDto(), authorId, isReferent: false, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var updated = await db.GeneralCalendarEvents.SingleAsync(e => e.Id == ev.Id);
        Assert.Equal("Título actualizado", updated.Title);
        Assert.Equal("Descripción actualizada", updated.Description);

        var log = Assert.Single(db.AuditLogs);
        Assert.Equal(authorId, log.UserId);
        Assert.Equal("Evento general modificado", log.Action);
        Assert.Equal($"GeneralCalendarEvent:{ev.Id}", log.AffectedEntity);
    }

    [Fact]
    public async Task UpdateAsync_WhenReferentUpdatesOtherUsersEvent_SucceedsAndLogsAudit()
    {
        using var db = CreateDbContext();
        var authorId = await SeedUserAsync(db, "Author");
        var referentId = await SeedUserAsync(db, "Referente");
        var ev = await SeedEventAsync(db, authorId);
        var service = new GeneralCalendarEventService(db);

        var result = await service.UpdateAsync(ev.Id, NewUpdateDto("Modificado por referente"), referentId, isReferent: true, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var updated = await db.GeneralCalendarEvents.SingleAsync(e => e.Id == ev.Id);
        Assert.Equal("Modificado por referente", updated.Title);

        var log = Assert.Single(db.AuditLogs);
        Assert.Equal(referentId, log.UserId);
        Assert.Equal("Evento general modificado", log.Action);
    }

    [Fact]
    public async Task UpdateAsync_WhenNonAuthorAndNonReferentAttempts_ReturnsForbidden()
    {
        using var db = CreateDbContext();
        var authorId = await SeedUserAsync(db, "Author");
        var otherUserId = await SeedUserAsync(db, "OtroUsuario");
        var ev = await SeedEventAsync(db, authorId);
        var service = new GeneralCalendarEventService(db);

        var result = await service.UpdateAsync(ev.Id, NewUpdateDto(), otherUserId, isReferent: false, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalendarEventOperationError.Forbidden, result.Error);
        Assert.Empty(db.AuditLogs);
    }

    [Fact]
    public async Task UpdateAsync_WhenEventNotFound_ReturnsNotFound()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var service = new GeneralCalendarEventService(db);

        var result = await service.UpdateAsync(Guid.NewGuid(), NewUpdateDto(), actorId, isReferent: true, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalendarEventOperationError.NotFound, result.Error);
        Assert.Empty(db.AuditLogs);
    }

    [Fact]
    public async Task DeleteAsync_WhenAuthorDeletes_SucceedsRemovesEntityAndLogsAudit()
    {
        using var db = CreateDbContext();
        var authorId = await SeedUserAsync(db, "Author");
        var ev = await SeedEventAsync(db, authorId);
        var service = new GeneralCalendarEventService(db);

        var result = await service.DeleteAsync(ev.Id, authorId, isReferent: false, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(await db.GeneralCalendarEvents.AnyAsync(e => e.Id == ev.Id));

        var log = Assert.Single(db.AuditLogs);
        Assert.Equal(authorId, log.UserId);
        Assert.Equal("Evento general eliminado", log.Action);
        Assert.Equal($"GeneralCalendarEvent:{ev.Id}", log.AffectedEntity);
    }

    [Fact]
    public async Task DeleteAsync_WhenReferentDeletes_SucceedsRemovesEntityAndLogsAudit()
    {
        using var db = CreateDbContext();
        var authorId = await SeedUserAsync(db, "Author");
        var referentId = await SeedUserAsync(db, "Referente");
        var ev = await SeedEventAsync(db, authorId);
        var service = new GeneralCalendarEventService(db);

        var result = await service.DeleteAsync(ev.Id, referentId, isReferent: true, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(await db.GeneralCalendarEvents.AnyAsync(e => e.Id == ev.Id));

        var log = Assert.Single(db.AuditLogs);
        Assert.Equal(referentId, log.UserId);
        Assert.Equal("Evento general eliminado", log.Action);
    }

    [Fact]
    public async Task DeleteAsync_WhenNonAuthorAndNonReferentAttempts_ReturnsForbidden()
    {
        using var db = CreateDbContext();
        var authorId = await SeedUserAsync(db, "Author");
        var otherUserId = await SeedUserAsync(db, "OtroUsuario");
        var ev = await SeedEventAsync(db, authorId);
        var service = new GeneralCalendarEventService(db);

        var result = await service.DeleteAsync(ev.Id, otherUserId, isReferent: false, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalendarEventOperationError.Forbidden, result.Error);
        Assert.True(await db.GeneralCalendarEvents.AnyAsync(e => e.Id == ev.Id));
        Assert.Empty(db.AuditLogs);
    }

    [Fact]
    public async Task DeleteAsync_WhenEventNotFound_ReturnsNotFound()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var service = new GeneralCalendarEventService(db);

        var result = await service.DeleteAsync(Guid.NewGuid(), actorId, isReferent: true, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalendarEventOperationError.NotFound, result.Error);
        Assert.Empty(db.AuditLogs);
    }
}