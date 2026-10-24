using Vicaria.Domain.Entities;

namespace Vicaria.Application.CaritasCatalogs;

public interface ICaritasCatalogService
{
    Task<IReadOnlyList<CaritasCatalogValueDto>> GetValuesAsync(CaritasCatalogCategory? category, bool onlyActive, CancellationToken cancellationToken = default);
    Task<CaritasCatalogOperationResult> CreateAsync(CreateCaritasCatalogValueDto dto, Guid actorId, CancellationToken cancellationToken = default);
    Task<CaritasCatalogOperationResult> UpdateAsync(Guid id, UpdateCaritasCatalogValueDto dto, Guid actorId, CancellationToken cancellationToken = default);
    Task<CaritasCatalogOperationResult> ToggleStatusAsync(Guid id, bool isActive, Guid actorId, CancellationToken cancellationToken = default);
    Task<CaritasCatalogOperationResult> DeleteAsync(Guid id, Guid actorId, CancellationToken cancellationToken = default);
}
