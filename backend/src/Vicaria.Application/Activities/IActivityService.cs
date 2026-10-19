namespace Vicaria.Application.Activities;

public interface IActivityService
{
    Task<IReadOnlyList<ActivityDto>> ListAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<GetActivityByIdResult> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CreateActivityResult> CreateAsync(CreateActivityDto dto, Guid actorId, CancellationToken cancellationToken = default);
    Task<UpdateActivityResult> UpdateAsync(Guid id, UpdateActivityDto dto, Guid actorId, CancellationToken cancellationToken = default);
    Task<DeleteActivityResult> DeleteAsync(Guid id, Guid actorId, CancellationToken cancellationToken = default);
}