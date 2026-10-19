namespace Vicaria.Application.Activities;

public record SyncCalendarActivitiesResult(
    int SyncedCount,
    IReadOnlyList<string> SyncedActivityNames
);