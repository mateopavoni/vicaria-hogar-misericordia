using Microsoft.EntityFrameworkCore;
using Vicaria.Application.Attendances;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.Infrastructure.Attendances;

// cálculo compartido de estadísticas de asistencia (SCRUM-225): lo usan el
// endpoint de inactivos y el PDF del resumen mensual; SCRUM-224 lo reutiliza
// para no tener dos fuentes de cálculo distintas del mismo resumen
public class AttendanceStatisticsService : IAttendanceStatisticsService
{
    private const int InactiveThresholdDays = 14;

    private readonly VicariaDbContext _dbContext;

    public AttendanceStatisticsService(VicariaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetInactivePeopleResult> GetInactivePeopleAsync(int days, DateTime? asOfDate = null, CancellationToken cancellationToken = default)
    {
        var asOf = (asOfDate ?? DateTime.UtcNow).Date;
        var inactive = await FindInactiveAsync(days, asOf, null, cancellationToken);
        return GetInactivePeopleResult.Ok(inactive);
    }

    public async Task<GetMonthlySummaryResult> GetMonthlySummaryAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        var monthStart = new DateTime(year, month, 1);
        var nextMonthStart = monthStart.AddMonths(1);
        var monthEnd = nextMonthStart.AddDays(-1);
        var daysInMonth = DateTime.DaysInMonth(year, month);

        var records = await _dbContext.Attendances
            .AsNoTracking()
            .Where(a => a.Date >= monthStart && a.Date < nextMonthStart)
            .ToListAsync(cancellationToken);

        var previousMonthTotal = await _dbContext.Attendances
            .AsNoTracking()
            .CountAsync(a => a.Date >= monthStart.AddMonths(-1) && a.Date < monthStart, cancellationToken);

        var totalsByActivity = Enum.GetValues<AttendanceActivity>()
            .Select(activity => new ActivityTotalDto(
                activity,
                GetActivityName(activity),
                records.Count(r => r.Activity == activity)))
            .ToList();

        double? comparisonPercentage = null;
        if (previousMonthTotal > 0)
        {
            comparisonPercentage = Math.Round((records.Count - previousMonthTotal) / (double)previousMonthTotal * 100, 2);
        }

        var existingPersonIds = await _dbContext.People
            .AsNoTracking()
            .Where(p => p.CreatedAt < nextMonthStart)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var inactive = await FindInactiveAsync(InactiveThresholdDays, monthEnd, existingPersonIds, cancellationToken);

        var summary = new MonthlyAttendanceSummaryDto(
            year,
            month,
            records.Count,
            records.Sum(r => r.AnonymousCount),
            Math.Round((records.Count + records.Sum(r => r.AnonymousCount)) / (double)daysInMonth, 2),
            totalsByActivity,
            previousMonthTotal,
            comparisonPercentage,
            inactive);

        return GetMonthlySummaryResult.Ok(summary);
    }

    private async Task<IReadOnlyList<InactivePersonDto>> FindInactiveAsync(int days, DateTime asOf, IEnumerable<Guid>? personIds, CancellationToken cancellationToken)
    {
        var threshold = asOf.AddDays(-days);

        var peopleQuery = _dbContext.People.AsNoTracking();
        if (personIds is not null)
        {
            var idSet = personIds.ToHashSet();
            peopleQuery = peopleQuery.Where(p => idSet.Contains(p.Id));
        }

        var people = await peopleQuery
            .Select(p => new { p.Id, p.FirstName, p.LastName })
            .ToListAsync(cancellationToken);

        var lastAttendances = await _dbContext.Attendances
            .AsNoTracking()
            .Where(a => a.Date < asOf.AddDays(1))
            .GroupBy(a => a.PersonId)
            .Select(g => new { PersonId = g.Key, LastDate = g.Max(a => a.Date) })
            .ToListAsync(cancellationToken);

        var lastByPerson = lastAttendances.ToDictionary(x => x.PersonId, x => x.LastDate.Date);

        return people
            .Where(p => !lastByPerson.TryGetValue(p.Id, out var lastDate) || lastDate < threshold)
            .Select(p =>
            {
                DateTime? lastDate = lastByPerson.TryGetValue(p.Id, out var value) ? value : null;
                int? daysSince = lastDate.HasValue ? (asOf - lastDate.Value).Days : null;
                return new InactivePersonDto(p.Id, p.FirstName, p.LastName, lastDate, daysSince);
            })
            .OrderByDescending(p => p.DaysSinceLastAttendance ?? int.MaxValue)
            .ToList();
    }

    private static string GetActivityName(AttendanceActivity activity) =>
        activity switch
        {
            AttendanceActivity.DayCenter => "Centro de Día",
            AttendanceActivity.CasaConvivencia => "Casa de Convivencia",
            _ => activity.ToString()
        };
}
