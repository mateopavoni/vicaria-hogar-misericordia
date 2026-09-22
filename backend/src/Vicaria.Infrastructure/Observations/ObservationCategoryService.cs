using Microsoft.EntityFrameworkCore;
using Vicaria.Application.Observations;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.Infrastructure.Observations;

public class ObservationCategoryService : IObservationCategoryService
{
    private readonly VicariaDbContext _dbContext;

    public ObservationCategoryService(VicariaDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<ObservationCategoryDto>> GetCategoriesAsync(bool onlyActive, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ObservationCategories.AsNoTracking();
        if (onlyActive) query = query.Where(c => c.IsActive);
        return await query.OrderBy(c => c.Name).Select(c => new ObservationCategoryDto(c.Id, c.Name, c.IsActive, c.CreatedAt)).ToListAsync(cancellationToken);
    }

    public async Task<CategoryOperationResult> CreateAsync(CreateObservationCategoryDto dto, CancellationToken cancellationToken = default)
    {
        var trimmedName = dto.Name.Trim();
        var exists = await _dbContext.ObservationCategories.AnyAsync(c => c.Name.ToLower() == trimmedName.ToLower(), cancellationToken);
        if (exists) return CategoryOperationResult.DuplicateName();

        var category = new ObservationCategory { Id = Guid.NewGuid(), Name = trimmedName, IsActive = true, CreatedAt = DateTime.UtcNow };
        _dbContext.ObservationCategories.Add(category);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CategoryOperationResult.Ok(new ObservationCategoryDto(category.Id, category.Name, category.IsActive, category.CreatedAt));
    }

    public async Task<CategoryOperationResult> UpdateAsync(Guid id, UpdateObservationCategoryDto dto, CancellationToken cancellationToken = default)
    {
        var category = await _dbContext.ObservationCategories.FindAsync([id], cancellationToken);
        if (category is null) return CategoryOperationResult.NotFound();

        var trimmedName = dto.Name.Trim();
        var exists = await _dbContext.ObservationCategories.AnyAsync(c => c.Id != id && c.Name.ToLower() == trimmedName.ToLower(), cancellationToken);
        if (exists) return CategoryOperationResult.DuplicateName();

        category.Name = trimmedName;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CategoryOperationResult.Ok(new ObservationCategoryDto(category.Id, category.Name, category.IsActive, category.CreatedAt));
    }

    public async Task<CategoryOperationResult> ToggleStatusAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var category = await _dbContext.ObservationCategories.FindAsync([id], cancellationToken);
        if (category is null) return CategoryOperationResult.NotFound();

        category.IsActive = isActive;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return CategoryOperationResult.Ok();
    }
}