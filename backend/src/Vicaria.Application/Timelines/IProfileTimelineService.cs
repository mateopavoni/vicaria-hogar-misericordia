namespace Vicaria.Application.Timelines;

public interface IProfileTimelineService
{
    Task<ProfileTimelineResult> GetTimelineAsync(
        Guid personId,
        CancellationToken cancellationToken = default);
}