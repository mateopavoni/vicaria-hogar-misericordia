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

    private static async Task ClearSeedTemplatesAsync(VicariaDbContext db)
    {
        db.GeneralCalendarEvents.RemoveRange(db.GeneralCalendarEvents);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetOccurrencesAsync_ExpandsTemplatesAndConcreteEvents()
    {
        using var db = CreateDbContext();
        await ClearSeedTemplatesAsync(db);
        db.GeneralCalendarEvents.AddRange(
            new GeneralCalendarEvent
            {
                Id = Guid.NewGuid(),
                Title = "Plantilla",
                Date = null,
                RecurrenceDays = WeekDays.Monday | WeekDays.Wednesday,
                CreatedAt = DateTime.UtcNow
            },
            new GeneralCalendarEvent
            {
                Id = Guid.NewGuid(),
                Title = "Concreto",
                Date = new DateTime(2026, 10, 10),
                RecurrenceDays = WeekDays.None,
                CreatedAt = DateTime.UtcNow
            },
            new GeneralCalendarEvent
            {
                Id = Guid.NewGuid(),
                Title = "Fuera de rango",
                Date = new DateTime(2026, 11, 5),
                RecurrenceDays = WeekDays.None,
                CreatedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        var result = await new GeneralCalendarEventService(db).GetOccurrencesAsync(
            new DateTime(2026, 10, 5), new DateTime(2026, 10, 11), 1, CancellationToken.None);

        Assert.Equal(3, result.Total);
        Assert.Equal(
            new[] { new DateTime(2026, 10, 5), new DateTime(2026, 10, 7), new DateTime(2026, 10, 10) },
            result.Items.Select(o => o.Date).ToList());
        Assert.DoesNotContain(result.Items, o => o.Title == "Fuera de rango");
    }

    [Fact]
    public async Task GetOccurrencesAsync_PaginatesExpandedOccurrences()
    {
        using var db = CreateDbContext();
        await ClearSeedTemplatesAsync(db);
        for (var i = 0; i < 12; i++)
        {
            db.GeneralCalendarEvents.Add(new GeneralCalendarEvent
            {
                Id = Guid.NewGuid(),
                Title = $"Evento {i}",
                Date = new DateTime(2026, 10, 10),
                RecurrenceDays = WeekDays.None,
                CreatedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();

        var result = await new GeneralCalendarEventService(db).GetOccurrencesAsync(
            new DateTime(2026, 10, 5), new DateTime(2026, 10, 11), 2, CancellationToken.None);

        Assert.Equal(12, result.Total);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingEvent_ReturnsDetailWithAuthorName()
    {
        using var db = CreateDbContext();
        var actorId = Guid.NewGuid();
        db.Users.Add(new User { Id = actorId, FirstName = "Author", LastName = "Test", Email = "author@test.com" });
        await db.SaveChangesAsync();

        var created = await new GeneralCalendarEventService(db).CreateAsync(
            new CreateGeneralCalendarEventDto(
                Title: "Reunión",
                Date: new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc)),
            actorId,
            CancellationToken.None);

        var result = await new GeneralCalendarEventService(db).GetByIdAsync(
            created.GeneralCalendarEventId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Reunión", result!.Title);
        Assert.Equal(actorId, result.AuthorUserId);
        Assert.Equal("Author Test", result.AuthorName);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        using var db = CreateDbContext();

        var result = await new GeneralCalendarEventService(db).GetByIdAsync(
            Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }
}
