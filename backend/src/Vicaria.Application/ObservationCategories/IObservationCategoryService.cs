namespace Vicaria.Application.ObservationCategories;

public interface IObservationCategoryService
{
    Task<CreateObservationCategoryResult> CreateAsync(CreateObservationCategoryDto dto, Guid actorId, CancellationToken cancellationToken = default);
    Task<UpdateObservationCategoryResult> UpdateAsync(Guid categoryId, UpdateObservationCategoryDto dto, Guid actorId, CancellationToken cancellationToken = default);
    Task<DeactivateObservationCategoryResult> DeactivateAsync(Guid categoryId, Guid actorId, CancellationToken cancellationToken = default);
}