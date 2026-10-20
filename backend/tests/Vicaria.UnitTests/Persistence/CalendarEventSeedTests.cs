using Microsoft.EntityFrameworkCore;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.Persistence;

public class CalendarEventSeedTests
{
    private static VicariaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<VicariaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new VicariaDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public void SeedContainsThreeGeneralActivityTemplates()
    {
        using var db = CreateDbContext();

        var templates = db.GeneralCalendarEvents.ToList();

        Assert.Equal(3, templates.Count);
        Assert.Contains(templates, e => e.Title == "Desayuno");
        Assert.Contains(templates, e => e.Title == "Almuerzo");
        Assert.Contains(templates, e => e.Title == "Merendero");
    }

    [Fact]
    public void BreakfastAndLunchAreScheduledMondayToFriday()
    {
        using var db = CreateDbContext();

        var expectedDays =
            WeekDays.Monday | WeekDays.Tuesday | WeekDays.Wednesday | WeekDays.Thursday | WeekDays.Friday;

        var breakfast = db.GeneralCalendarEvents.Single(e => e.Title == "Desayuno");
        var lunch = db.GeneralCalendarEvents.Single(e => e.Title == "Almuerzo");

        Assert.Equal(expectedDays, breakfast.RecurrenceDays);
        Assert.Equal(new TimeSpan(9, 30, 0), breakfast.StartTime);
        Assert.Equal(new TimeSpan(11, 0, 0), breakfast.EndTime);

        Assert.Equal(expectedDays, lunch.RecurrenceDays);
        Assert.Equal(new TimeSpan(13, 30, 0), lunch.StartTime);
        Assert.Equal(new TimeSpan(14, 30, 0), lunch.EndTime);
    }

    [Fact]
    public void SnackIsScheduledTuesdayAfternoon()
    {
        using var db = CreateDbContext();

        var snack = db.GeneralCalendarEvents.Single(e => e.Title == "Merendero");

        Assert.Equal(WeekDays.Tuesday, snack.RecurrenceDays);
        Assert.Equal(new TimeSpan(16, 0, 0), snack.StartTime);
        Assert.Equal(new TimeSpan(18, 0, 0), snack.EndTime);
    }

    [Fact]
    public void TemplatesHaveNoDateAndNoAuthor()
    {
        using var db = CreateDbContext();

        var templates = db.GeneralCalendarEvents.ToList();

        Assert.All(templates, e => Assert.Null(e.Date));
        Assert.All(templates, e => Assert.Null(e.AuthorUserId));
        Assert.All(templates, e => Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), e.CreatedAt));
    }

    [Fact]
    public void PersonalCalendarEventsStartEmpty()
    {
        using var db = CreateDbContext();

        Assert.Empty(db.PersonalCalendarEvents.ToList());
    }
}
