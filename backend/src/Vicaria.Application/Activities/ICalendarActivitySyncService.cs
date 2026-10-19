namespace Vicaria.Application.Activities;

public interface ICalendarActivitySyncService
{
    Task<SyncCalendarActivitiesResult> SyncRecurringActivitiesAsync(Guid actorId, CancellationToken cancellationToken = default);
}