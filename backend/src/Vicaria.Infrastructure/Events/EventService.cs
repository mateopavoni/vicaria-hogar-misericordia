using Microsoft.EntityFrameworkCore;
using Vicaria.Application.Events;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.Infrastructure.Events;

public class EventService : IEventService
{
    private readonly VicariaDbContext _dbContext;

    public EventService(VicariaDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<EventOccurrenceDto>> GetEventsByRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var rawEvents = await _dbContext.Events
            .AsNoTracking()
            .Include(e => e.AuthorUser)
            .Where(e => (!e.IsRecurring && e.StartDate <= toDate && e.EndDate >= fromDate) ||
                        (e.IsRecurring && e.StartDate <= toDate && (!e.RecurrenceUntil.HasValue || e.RecurrenceUntil.Value >= fromDate)))
            .ToListAsync(cancellationToken);

        var occurrences = new List<EventOccurrenceDto>();

        foreach (var ev in rawEvents)
        {
            var authorName = $"{ev.AuthorUser.FirstName} {ev.AuthorUser.LastName}".Trim();

            if (!ev.IsRecurring)
            {
                occurrences.Add(new EventOccurrenceDto(
                    ev.Id,
                    ev.Title,
                    ev.Description,
                    ev.StartDate,
                    ev.EndDate,
                    false,
                    ev.AuthorUserId,
                    authorName
                ));
                continue;
            }

            var duration = ev.EndDate - ev.StartDate;
            var currentDay = fromDate.Date > ev.StartDate.Date ? fromDate.Date : ev.StartDate.Date;
            var maxLimit = ev.RecurrenceUntil.HasValue && ev.RecurrenceUntil.Value.Date < toDate.Date
                ? ev.RecurrenceUntil.Value.Date
                : toDate.Date;

            while (currentDay <= maxLimit)
            {
                var matchesPattern = ev.RecurrencePattern switch
                {
                    EventRecurrencePattern.Daily => true,
                    EventRecurrencePattern.Weekly => ev.RecurrenceDays == null || ev.RecurrenceDays.Length == 0 || ev.RecurrenceDays.Contains(currentDay.DayOfWeek),
                    EventRecurrencePattern.Monthly => currentDay.Day == ev.StartDate.Day,
                    _ => false
                };

                if (matchesPattern)
                {
                    var occStart = currentDay.Add(ev.StartDate.TimeOfDay);
                    var occEnd = occStart.Add(duration);

                    if (occStart <= toDate && occEnd >= fromDate)
                    {
                        occurrences.Add(new EventOccurrenceDto(
                            ev.Id,
                            ev.Title,
                            ev.Description,
                            occStart,
                            occEnd,
                            true,
                            ev.AuthorUserId,
                            authorName
                        ));
                    }
                }

                currentDay = currentDay.AddDays(1);
            }
        }

        return occurrences.OrderBy(o => o.StartDate).ToList();
    }

    public async Task<EventDetailDto?> GetEventByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var ev = await _dbContext.Events
            .AsNoTracking()
            .Include(e => e.AuthorUser)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (ev is null) return null;

        var authorName = $"{ev.AuthorUser.FirstName} {ev.AuthorUser.LastName}".Trim();

        return new EventDetailDto(
            ev.Id,
            ev.Title,
            ev.Description,
            ev.StartDate,
            ev.EndDate,
            ev.IsRecurring,
            ev.RecurrencePattern,
            ev.RecurrenceDays,
            ev.RecurrenceUntil,
            ev.AuthorUserId,
            authorName,
            ev.CreatedAt
        );
    }
}