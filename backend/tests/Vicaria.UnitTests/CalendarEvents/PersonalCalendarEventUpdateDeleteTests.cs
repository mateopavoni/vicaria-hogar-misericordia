using Microsoft.EntityFrameworkCore;
using Vicaria.Application.CalendarEvents;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.CalendarEvents;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.CalendarEvents;

public class PersonalCalendarEventUpdateDeleteTests
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

    private static async Task<PersonalCalendarEvent> SeedEventAsync(VicariaDbContext db, Guid authorId)
    {
        var calendarEvent = new PersonalCalendarEvent
        {
            Id = Guid.NewGuid(),
            Title = "Original",
            Date = DateTime.UtcNow.Date.AddDays(1),
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(10, 0, 0),
            RecurrenceDays = WeekDays.None,
            AuthorUserId = authorId,
            CreatedAt = DateTime.UtcNow
        };
        db.PersonalCalendarEvents.Add(calendarEvent);
        await db.SaveChangesAsync();
        return calendarEvent;
    }

    private static UpdateGeneralCalendarEventDto NewValues() => new(
        "  Editado  ", "  Detalle  ", DateTime.UtcNow.Date.AddDays(2),
        new TimeSpan(14, 0, 0), new TimeSpan(15, 30, 0), WeekDays.Monday);

    [Fact]
    public async Task UpdateAsync_WhenAuthor_UpdatesFieldsAndLogsAudit()
    {
        using var db = CreateDbContext();
        var authorId = Guid.NewGuid();
        var calendarEvent = await SeedEventAsync(db, authorId);

        var result = await new PersonalCalendarEventService(db)
            .UpdateAsync(calendarEvent.Id, NewValues(), authorId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var saved = await db.PersonalCalendarEvents.SingleAsync();
        Assert.Equal("Editado", saved.Title);
        Assert.Equal("Detalle", saved.Description);
        Assert.Equal(new TimeSpan(14, 0, 0), saved.StartTime);
        Assert.Equal(WeekDays.Monday, saved.RecurrenceDays);

        var log = Assert.Single(db.AuditLogs);
        Assert.Equal(authorId, log.UserId);
        Assert.Equal("Evento personal modificado", log.Action);
        Assert.Equal($"PersonalCalendarEvent:{calendarEvent.Id}", log.AffectedEntity);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotAuthor_ReturnsNotFoundAndKeepsEvent()
    {
        using var db = CreateDbContext();
        var calendarEvent = await SeedEventAsync(db, Guid.NewGuid());

        var result = await new PersonalCalendarEventService(db)
            .UpdateAsync(calendarEvent.Id, NewValues(), Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(CalendarEventOperationError.NotFound, result.Error);
        Assert.Equal("Original", (await db.PersonalCalendarEvents.SingleAsync()).Title);
        Assert.Empty(db.AuditLogs);
    }

    [Fact]
    public async Task DeleteAsync_WhenAuthor_RemovesEventAndLogsAudit()
    {
        using var db = CreateDbContext();
        var authorId = Guid.NewGuid();
        var calendarEvent = await SeedEventAsync(db, authorId);

        var result = await new PersonalCalendarEventService(db)
            .DeleteAsync(calendarEvent.Id, authorId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(db.PersonalCalendarEvents);
        var log = Assert.Single(db.AuditLogs);
        Assert.Equal("Evento personal eliminado", log.Action);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotAuthor_ReturnsNotFoundAndKeepsEvent()
    {
        using var db = CreateDbContext();
        var calendarEvent = await SeedEventAsync(db, Guid.NewGuid());

        var result = await new PersonalCalendarEventService(db)
            .DeleteAsync(calendarEvent.Id, Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(CalendarEventOperationError.NotFound, result.Error);
        Assert.Single(db.PersonalCalendarEvents);
    }
}
