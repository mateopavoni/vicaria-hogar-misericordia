using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.CalendarEvents;

namespace Vicaria.UnitTests.CalendarEvents;

// recurrencia mensual: el evento cae el mismo día del mes que su fecha de inicio
public class CalendarEventMonthlyRecurrenceTests
{
    private static DateTime D(int y, int m, int d) => new(y, m, d);

    [Fact]
    public void Expand_MonthlyEvent_ReturnsSameDayEachMonth()
    {
        var result = CalendarEventOccurrenceExpander.Expand(
            D(2026, 10, 5), WeekDays.None, D(2026, 10, 1), D(2027, 1, 31), repeatsMonthly: true);

        Assert.Equal([D(2026, 10, 5), D(2026, 11, 5), D(2026, 12, 5), D(2027, 1, 5)], result);
    }

    [Fact]
    public void Expand_MonthlyEventStartingBeforeRange_ShowsOccurrenceInsideRange()
    {
        var result = CalendarEventOccurrenceExpander.Expand(
            D(2026, 1, 15), WeekDays.None, D(2026, 6, 1), D(2026, 6, 30), repeatsMonthly: true);

        Assert.Equal([D(2026, 6, 15)], result);
    }

    [Fact]
    public void Expand_MonthlyEventOnDay31_FallsOnLastDayOfShorterMonths()
    {
        var result = CalendarEventOccurrenceExpander.Expand(
            D(2026, 1, 31), WeekDays.None, D(2026, 2, 1), D(2026, 4, 30), repeatsMonthly: true);

        Assert.Equal([D(2026, 2, 28), D(2026, 3, 31), D(2026, 4, 30)], result);
    }

    [Fact]
    public void Expand_MonthlyEventOnDay31_UsesLeapDayInLeapYear()
    {
        var result = CalendarEventOccurrenceExpander.Expand(
            D(2027, 12, 31), WeekDays.None, D(2028, 2, 1), D(2028, 2, 29), repeatsMonthly: true);

        Assert.Equal([D(2028, 2, 29)], result);
    }

    [Fact]
    public void Expand_MonthlyEvent_NeverReturnsDatesBeforeItsStart()
    {
        var result = CalendarEventOccurrenceExpander.Expand(
            D(2026, 10, 20), WeekDays.None, D(2026, 10, 1), D(2026, 10, 19), repeatsMonthly: true);

        Assert.Empty(result);
    }

    [Fact]
    public void Expand_MonthlyEvent_RangeWithoutThatDayReturnsEmpty()
    {
        var result = CalendarEventOccurrenceExpander.Expand(
            D(2026, 10, 20), WeekDays.None, D(2026, 11, 1), D(2026, 11, 19), repeatsMonthly: true);

        Assert.Empty(result);
    }

    [Fact]
    public void Expand_WithoutMonthlyFlag_StillSingleDate()
    {
        var result = CalendarEventOccurrenceExpander.Expand(
            D(2026, 10, 5), WeekDays.None, D(2026, 10, 1), D(2026, 12, 31));

        Assert.Equal([D(2026, 10, 5)], result);
    }
}
