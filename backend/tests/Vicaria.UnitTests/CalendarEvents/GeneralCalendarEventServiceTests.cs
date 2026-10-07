using Microsoft.EntityFrameworkCore;
using Vicaria.Application.CalendarEvents;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.CalendarEvents;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.CalendarEvents;

public class GeneralCalendarEventServiceTests
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

    [Fact]
    public async Task CreateAsync_WithValidDto_PersistsEventWithAuthorAndUtcCreatedAt()
    {
        using var db = CreateDbContext();
        var actorId = Guid.NewGuid();
        db.Users.Add(new User { Id = actorId, FirstName = "Author", LastName = "Test", Email = "author@test.com" });
        await db.SaveChangesAsync();

        var dto = new CreateGeneralCalendarEventDto(
            Title: "  Merienda  ",
            Date: new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc),
            StartTime: new TimeSpan(16, 0, 0),
            EndTime: new TimeSpan(18, 0, 0),
            Description: "  Actividad en el patio  ",
            RecurrenceDays: WeekDays.Tuesday);

        var result = await new GeneralCalendarEventService(db).CreateAsync(dto, actorId, CancellationToken.None);

        Assert.True(result.Success);
        var stored = db.GeneralCalendarEvents.Single(e => e.Id == result.GeneralCalendarEventId);
        Assert.Equal("Merienda", stored.Title);
        Assert.Equal("Actividad en el patio", stored.Description);
        Assert.Equal(actorId, stored.AuthorUserId);
        Assert.Equal(WeekDays.Tuesday, stored.RecurrenceDays);
        Assert.Equal(DateTimeKind.Utc, stored.CreatedAt.Kind);
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_WritesAuditLogEntry()
    {
        using var db = CreateDbContext();
        var actorId = Guid.NewGuid();
        db.Users.Add(new User { Id = actorId, FirstName = "Author", LastName = "Test", Email = "author@test.com" });
        await db.SaveChangesAsync();

        var dto = new CreateGeneralCalendarEventDto(
            Title: "Reunión de equipo",
            Date: new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc));

        var result = await new GeneralCalendarEventService(db).CreateAsync(dto, actorId, CancellationToken.None);

        var log = db.AuditLogs.Single();
        Assert.Equal(actorId, log.UserId);
        Assert.Equal("Evento general creado", log.Action);
        Assert.Equal($"GeneralCalendarEvent:{result.GeneralCalendarEventId}", log.AffectedEntity);
    }

    [Fact]
    public async Task CreateAsync_WithoutOptionalFields_PersistsNulls()
    {
        using var db = CreateDbContext();
        var actorId = Guid.NewGuid();
        db.Users.Add(new User { Id = actorId, FirstName = "Author", LastName = "Test", Email = "author@test.com" });
        await db.SaveChangesAsync();

        var dto = new CreateGeneralCalendarEventDto(
            Title: "Reunión",
            Date: new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc));

        var result = await new GeneralCalendarEventService(db).CreateAsync(dto, actorId, CancellationToken.None);

        var stored = db.GeneralCalendarEvents.Single(e => e.Id == result.GeneralCalendarEventId);
        Assert.Null(stored.StartTime);
        Assert.Null(stored.EndTime);
        Assert.Null(stored.Description);
        Assert.Equal(WeekDays.None, stored.RecurrenceDays);
    }
}
