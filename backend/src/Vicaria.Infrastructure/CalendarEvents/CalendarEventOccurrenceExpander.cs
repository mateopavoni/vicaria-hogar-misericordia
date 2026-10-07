using Vicaria.Domain.Entities;

namespace Vicaria.Infrastructure.CalendarEvents;

// expansion de eventos recurrentes a ocurrencias concretas dentro de un rango (SCRUM-194):
// - Date null (plantillas institucionales del seed) → cada día del rango que matchee RecurrenceDays
// - Date con valor → la serie arranca en Date (primera ocurrencia siempre en Date) y desde ahí
//   se repite semanalmente los días marcados en RecurrenceDays; RecurrenceDays.None = fecha única
public static class CalendarEventOccurrenceExpander
{
    public static IReadOnlyList<DateTime> Expand(
        DateTime? date,
        WeekDays recurrenceDays,
        DateTime from,
        DateTime to)
    {
        var fromDate = from.Date;
        var toDate = to.Date;
        var dates = new List<DateTime>();

        if (date is null)
        {
            if (recurrenceDays == WeekDays.None)
            {
                return dates;
            }

            for (var day = fromDate; day <= toDate; day = day.AddDays(1))
            {
                if (Matches(recurrenceDays, day.DayOfWeek))
                {
                    dates.Add(day);
                }
            }

            return dates;
        }

        var startDate = date.Value.Date;
        if (startDate > toDate)
        {
            return dates;
        }

        if (startDate >= fromDate)
        {
            dates.Add(startDate);
        }

        var loopStart = startDate > fromDate ? startDate : fromDate;
        for (var day = loopStart; day <= toDate; day = day.AddDays(1))
        {
            if (day == startDate)
            {
                continue;
            }

            if (Matches(recurrenceDays, day.DayOfWeek))
            {
                dates.Add(day);
            }
        }

        return dates;
    }

    private static bool Matches(WeekDays recurrenceDays, DayOfWeek dayOfWeek) =>
        recurrenceDays.HasFlag(WeekDayFor(dayOfWeek));

    private static WeekDays WeekDayFor(DayOfWeek dayOfWeek) => dayOfWeek switch
    {
        DayOfWeek.Monday => WeekDays.Monday,
        DayOfWeek.Tuesday => WeekDays.Tuesday,
        DayOfWeek.Wednesday => WeekDays.Wednesday,
        DayOfWeek.Thursday => WeekDays.Thursday,
        DayOfWeek.Friday => WeekDays.Friday,
        DayOfWeek.Saturday => WeekDays.Saturday,
        DayOfWeek.Sunday => WeekDays.Sunday,
        _ => WeekDays.None
    };
}
