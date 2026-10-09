using Microsoft.EntityFrameworkCore;
using Vicaria.Application.CalendarEvents;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.CalendarEvents;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.CalendarEvents;

public class PersonalCalendarEventServiceTests
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

    private static async Task<(Guid OwnerId, Guid OtherId)> SeedTwoUsersAsync(VicariaDbContext db)
    {
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        db.Users.AddRange(
            new User { Id = ownerId, FirstName = "Owner", LastName = "Test", Email = "owner@test.com" },
            new User { Id = otherId, FirstName = "Other", LastName = "Test", Email = "other@test.com" });
        await db.SaveChangesAsync();
        return (ownerId, otherId);
    }

    private static PersonalCalendarEvent NewEvent(Guid authorId, string title, DateTime? date, WeekDays recurrenceDays = WeekDays.None) => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        Date = date,
        RecurrenceDays = recurrenceDays,
        AuthorUserId = authorId,
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task GetOccurrencesAsync_ReturnsOnlyEventsOwnedByActor()
    {
        using var db = CreateDbContext();
        var (ownerId, otherId) = await SeedTwoUsersAsync(db);
        db.PersonalCalendarEvents.AddRange(
            NewEvent(ownerId, "Mi evento", new DateTime(2026, 10, 6)),
            NewEvent(otherId, "Evento de otro usuario", new DateTime(2026, 10, 6)));
        await db.SaveChangesAsync();

        var result = await new PersonalCalendarEventService(db).GetOccurrencesAsync(
            new DateTime(2026, 10, 5), new DateTime(2026, 10, 11), 1, ownerId, CancellationToken.None);

        Assert.Equal(1, result.Total);
        Assert.Equal("Mi evento", Assert.Single(result.Items).Title);
    }

    [Fact]
    public async Task GetOccurrencesAsync_PaginatesExpandedOccurrences()
    {
        using var db = CreateDbContext();
        var (ownerId, _) = await SeedTwoUsersAsync(db);
        db.PersonalCalendarEvents.Add(NewEvent(
            ownerId, "Todos los días", null, WeekDays.Monday | WeekDays.Tuesday | WeekDays.Wednesday |
            WeekDays.Thursday | WeekDays.Friday | WeekDays.Saturday | WeekDays.Sunday));
        await db.SaveChangesAsync();

        var result = await new PersonalCalendarEventService(db).GetOccurrencesAsync(
            new DateTime(2026, 10, 5), new DateTime(2026, 10, 31), 2, ownerId, CancellationToken.None);

        Assert.Equal(27, result.Total);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(10, result.Items.Count);
    }

    [Fact]
    public async Task GetOccurrencesAsync_WithNoOwnedEvents_ReturnsEmptyPage()
    {
        using var db = CreateDbContext();
        var (ownerId, otherId) = await SeedTwoUsersAsync(db);
        db.PersonalCalendarEvents.Add(NewEvent(otherId, "Evento ajeno", new DateTime(2026, 10, 6)));
        await db.SaveChangesAsync();

        var result = await new PersonalCalendarEventService(db).GetOccurrencesAsync(
            new DateTime(2026, 10, 5), new DateTime(2026, 10, 11), 1, ownerId, CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.Total);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task GetByIdAsync_OwnEvent_ReturnsDetail()
    {
        using var db = CreateDbContext();
        var (ownerId, _) = await SeedTwoUsersAsync(db);
        var calendarEvent = NewEvent(ownerId, "Mi evento", new DateTime(2026, 10, 6));
        db.PersonalCalendarEvents.Add(calendarEvent);
        await db.SaveChangesAsync();

        var result = await new PersonalCalendarEventService(db).GetByIdAsync(
            calendarEvent.Id, ownerId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(calendarEvent.Id, result!.Id);
        Assert.Equal("Mi evento", result.Title);
        Assert.Equal(ownerId, result.AuthorUserId);
    }

    [Fact]
    public async Task GetByIdAsync_OtherUsersEvent_ReturnsNull()
    {
        using var db = CreateDbContext();
        var (ownerId, otherId) = await SeedTwoUsersAsync(db);
        var foreignEvent = NewEvent(otherId, "Evento de otro", new DateTime(2026, 10, 6));
        db.PersonalCalendarEvents.Add(foreignEvent);
        await db.SaveChangesAsync();

        var result = await new PersonalCalendarEventService(db).GetByIdAsync(
            foreignEvent.Id, ownerId, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        using var db = CreateDbContext();
        var (ownerId, _) = await SeedTwoUsersAsync(db);

        var result = await new PersonalCalendarEventService(db).GetByIdAsync(
            Guid.NewGuid(), ownerId, CancellationToken.None);

        Assert.Null(result);
    }


    [Fact]
    public async Task CreateAsync_StoresEventWithActorAsAuthor_AndOnlyOwnerSeesIt()
    {
        using var db = CreateDbContext();
        var (ownerId, otherId) = await SeedTwoUsersAsync(db);
        var service = new PersonalCalendarEventService(db);
        var dto = new CreateGeneralCalendarEventDto(
            "Turno médico", new DateTime(2026, 10, 12), new TimeSpan(9, 0, 0), new TimeSpan(10, 0, 0));

        var id = await service.CreateAsync(dto, ownerId, CancellationToken.None);

        var stored = await db.PersonalCalendarEvents.SingleAsync(e => e.Id == id);
        Assert.Equal(ownerId, stored.AuthorUserId);
        var own = await service.GetOccurrencesAsync(new DateTime(2026, 10, 12), new DateTime(2026, 10, 12), 1, ownerId, CancellationToken.None);
        var other = await service.GetOccurrencesAsync(new DateTime(2026, 10, 12), new DateTime(2026, 10, 12), 1, otherId, CancellationToken.None);
        Assert.Single(own.Items);
        Assert.Equal(ownerId, own.Items[0].AuthorUserId);
        Assert.Empty(other.Items);
    }
}
