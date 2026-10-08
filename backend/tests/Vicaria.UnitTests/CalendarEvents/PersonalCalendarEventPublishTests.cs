using Microsoft.EntityFrameworkCore;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.CalendarEvents;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.CalendarEvents;

public class PersonalCalendarEventPublishTests
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

    [Fact]
    public async Task PublishAsync_WhenAuthorPublishes_MovesEventToGeneralAndLogsAudit()
    {
        using var db = CreateDbContext();
        var authorId = await SeedUserAsync(db, "Dueño");

        var personalEvent = new PersonalCalendarEvent
        {
            Id = Guid.NewGuid(),
            Title = "Compromiso privado",
            Description = "Pasa a ser público",
            Date = DateTime.UtcNow.Date.AddDays(1),
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(11, 0, 0),
            RecurrenceDays = WeekDays.Monday,
            AuthorUserId = authorId,
            CreatedAt = DateTime.UtcNow
        };
        db.PersonalCalendarEvents.Add(personalEvent);
        await db.SaveChangesAsync();

        var service = new PersonalCalendarEventService(db);

        var result = await service.PublishAsync(personalEvent.Id, authorId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.GeneralCalendarEventId);

        Assert.False(await db.PersonalCalendarEvents.AnyAsync(e => e.Id == personalEvent.Id));

        var generalEvent = await db.GeneralCalendarEvents.SingleAsync(e => e.Id == result.GeneralCalendarEventId);
        Assert.Equal("Compromiso privado", generalEvent.Title);
        Assert.Equal("Pasa a ser público", generalEvent.Description);
        Assert.Equal(personalEvent.Date, generalEvent.Date);
        Assert.Equal(personalEvent.StartTime, generalEvent.StartTime);
        Assert.Equal(personalEvent.EndTime, generalEvent.EndTime);
        Assert.Equal(personalEvent.RecurrenceDays, generalEvent.RecurrenceDays);
        Assert.Equal(authorId, generalEvent.AuthorUserId);

        var log = Assert.Single(db.AuditLogs);
        Assert.Equal(authorId, log.UserId);
        Assert.Equal("Evento personal publicado como general", log.Action);
        Assert.Equal($"GeneralCalendarEvent:{generalEvent.Id}", log.AffectedEntity);
    }

    [Fact]
    public async Task PublishAsync_WhenEventBelongsToAnotherUser_ReturnsNotFoundAndDoesNotPublish()
    {
        using var db = CreateDbContext();
        var authorId = await SeedUserAsync(db, "Dueño");
        var otherUserId = await SeedUserAsync(db, "Otro");

        var personalEvent = new PersonalCalendarEvent
        {
            Id = Guid.NewGuid(),
            Title = "Evento ajeno",
            Date = DateTime.UtcNow.Date.AddDays(1),
            AuthorUserId = authorId,
            CreatedAt = DateTime.UtcNow
        };
        db.PersonalCalendarEvents.Add(personalEvent);
        await db.SaveChangesAsync();

        var service = new PersonalCalendarEventService(db);

        var result = await service.PublishAsync(personalEvent.Id, otherUserId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.GeneralCalendarEventId);
        Assert.Equal("El evento especificado no existe.", result.ErrorMessage);

        // No se elimina el evento personal original
        Assert.True(await db.PersonalCalendarEvents.AnyAsync(e => e.Id == personalEvent.Id));
        Assert.Empty(db.AuditLogs);
    }

    [Fact]
    public async Task PublishAsync_WhenEventDoesNotExist_ReturnsNotFound()
    {
        using var db = CreateDbContext();
        var actorId = await SeedUserAsync(db);
        var service = new PersonalCalendarEventService(db);

        var result = await service.PublishAsync(Guid.NewGuid(), actorId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.GeneralCalendarEventId);
        Assert.Equal("El evento especificado no existe.", result.ErrorMessage);
        Assert.Empty(db.AuditLogs);
    }
}