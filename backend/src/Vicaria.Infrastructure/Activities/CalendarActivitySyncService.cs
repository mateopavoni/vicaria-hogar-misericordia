using Microsoft.EntityFrameworkCore;
using Vicaria.Application.Activities;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.Infrastructure.Activities;

public class CalendarActivitySyncService : ICalendarActivitySyncService
{
    private readonly VicariaDbContext _dbContext;

    public CalendarActivitySyncService(VicariaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SyncCalendarActivitiesResult> SyncRecurringActivitiesAsync(Guid actorId, CancellationToken cancellationToken = default)
    {
        var recurringEvents = await _dbContext.GeneralCalendarEvents
            .AsNoTracking()
            .Where(e => e.RecurrenceDays != WeekDays.None || e.RepeatsMonthly || e.Date == null)
            .ToListAsync(cancellationToken);

        var recurringTitles = recurringEvents
            .Select(e => e.Title.Trim())
            .Where(title => !string.IsNullOrWhiteSpace(title))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingActivities = await _dbContext.Activities
            .AsNoTracking()
            .Where(a => a.IsActive)
            .Select(a => a.Name.ToLower())
            .ToListAsync(cancellationToken);

        var existingSet = existingActivities.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var newlySyncedNames = new List<string>();

        foreach (var title in recurringTitles)
        {
            if (existingSet.Contains(title))
            {
                continue;
            }

            var activity = new Activity
            {
                Id = Guid.NewGuid(),
                Name = title,
                IsActive = true,
                IsFromCalendar = true,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Activities.Add(activity);
            existingSet.Add(title);
            newlySyncedNames.Add(title);

            _dbContext.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = actorId,
                Action = $"Actividad sincronizada desde calendario recurrente: {activity.Name}",
                AffectedEntity = $"Activity:{activity.Id}",
                Date = DateTime.UtcNow
            });
        }

        if (newlySyncedNames.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return new SyncCalendarActivitiesResult(newlySyncedNames.Count, newlySyncedNames);
    }
}