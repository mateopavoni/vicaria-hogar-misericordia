namespace Vicaria.Application.Observations;

public interface IObservationCategoryService
{
    Task<IReadOnlyList<ObservationCategoryDto>> GetCategoriesAsync(bool onlyActive, CancellationToken cancellationToken = default);
    Task<CategoryOperationResult> CreateAsync(CreateObservationCategoryDto dto, CancellationToken cancellationToken = default);
    Task<CategoryOperationResult> UpdateAsync(Guid id, UpdateObservationCategoryDto dto, CancellationToken cancellationToken = default);
    Task<CategoryOperationResult> ToggleStatusAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);
}