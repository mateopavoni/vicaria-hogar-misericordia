using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.CalendarEvents;

namespace Vicaria.UnitTests.CalendarEvents;

public class CalendarEventOccurrenceExpanderTests
{
    private static readonly DateTime Monday = new(2026, 10, 5);
    private static readonly DateTime Sunday = new(2026, 10, 11);
    private static readonly DateTime TenDaysLater = new(2026, 10, 14);

    [Fact]
    public void Expand_NullDateWithWeekdayFlags_ExpandsEveryMatchedDayInRange()
    {
        var dates = CalendarEventOccurrenceExpander.Expand(
            null,
            WeekDays.Monday | WeekDays.Wednesday,
            Monday,
            TenDaysLater);

        Assert.Equal(
            new[] { new DateTime(2026, 10, 5), new DateTime(2026, 10, 7), new DateTime(2026, 10, 12), new DateTime(2026, 10, 14) },
            dates);
    }

    [Fact]
    public void Expand_NullDateWithoutRecurrence_ReturnsEmpty()
    {
        var dates = CalendarEventOccurrenceExpander.Expand(null, WeekDays.None, Monday, Sunday);

        Assert.Empty(dates);
    }

    [Fact]
    public void Expand_SingleDateWithoutRecurrence_ReturnsDateOnlyWhenInRange()
    {
        var inRange = CalendarEventOccurrenceExpander.Expand(
            new DateTime(2026, 10, 10), WeekDays.None, Monday, Sunday);
        var outOfRange = CalendarEventOccurrenceExpander.Expand(
            new DateTime(2026, 11, 1), WeekDays.None, Monday, Sunday);

        Assert.Equal(new[] { new DateTime(2026, 10, 10) }, inRange);
        Assert.Empty(outOfRange);
    }

    [Fact]
    public void Expand_SeriesStartsOnStartDate_AndSkipsEarlierMatchedDays()
    {
        // serie que arranca un sábado 10/10 con flags L-W: los días marcados anteriores
        // al inicio de la serie no expanden, solo los posteriores
        var dates = CalendarEventOccurrenceExpander.Expand(
            new DateTime(2026, 10, 10),
            WeekDays.Monday | WeekDays.Wednesday,
            Monday,
            TenDaysLater);

        Assert.Equal(
            new[] { new DateTime(2026, 10, 10), new DateTime(2026, 10, 12), new DateTime(2026, 10, 14) },
            dates);
    }

    [Fact]
    public void Expand_SeriesStartingBeforeRange_OnlyReturnsMatchedDaysInsideRange()
    {
        var dates = CalendarEventOccurrenceExpander.Expand(
            new DateTime(2026, 9, 30),
            WeekDays.Tuesday,
            Monday,
            new DateTime(2026, 10, 13));

        Assert.Equal(new[] { new DateTime(2026, 10, 6), new DateTime(2026, 10, 13) }, dates);
    }

    [Fact]
    public void Expand_SeriesStartingAfterRange_ReturnsEmpty()
    {
        var dates = CalendarEventOccurrenceExpander.Expand(
            new DateTime(2026, 12, 1),
            WeekDays.Monday,
            Monday,
            Sunday);

        Assert.Empty(dates);
    }

    [Fact]
    public void Expand_AppliedToWeekendOnlyEvent_OnlyMatchesWeekendDays()
    {
        var dates = CalendarEventOccurrenceExpander.Expand(
            null,
            WeekDays.Saturday | WeekDays.Sunday,
            Monday,
            Sunday);

        Assert.Equal(new[] { new DateTime(2026, 10, 10), new DateTime(2026, 10, 11) }, dates);
    }
}
