namespace Vicaria.Application.Observations;

public interface IObservationCategoryService
{
    Task<IReadOnlyList<ObservationCategoryDto>> GetCategoriesAsync(bool onlyActive, CancellationToken cancellationToken = default);
    Task<CategoryOperationResult> CreateAsync(CreateObservationCategoryDto dto, Guid actorId, CancellationToken cancellationToken = default);
    Task<CategoryOperationResult> UpdateAsync(Guid id, UpdateObservationCategoryDto dto, Guid actorId, CancellationToken cancellationToken = default);
    Task<CategoryOperationResult> ToggleStatusAsync(Guid id, bool isActive, Guid actorId, CancellationToken cancellationToken = default);
    Task<CategoryOperationResult> DeleteAsync(Guid id, Guid actorId, CancellationToken cancellationToken = default);
}
