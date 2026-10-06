using Vicaria.Domain.Entities;

namespace Vicaria.Application.LifeStories;

public interface ILifeStoryService
{
    Task<LifeStoryResponseDto> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default);
    Task<LifeStoryResponseDto?> UpdateAsync(Guid personId, UpdateLifeStoryDto dto, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<LifeStoryResponseDto?> UpdateStageAsync(Guid personId, LifeStoryStage stage, string content, Guid actorUserId, CancellationToken cancellationToken = default);
}