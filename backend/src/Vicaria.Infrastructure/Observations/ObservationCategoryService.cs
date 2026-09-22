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
        return await query.OrderBy(c => c.Name).Select(c => new ObservationCategoryDto(c.Id, c.Name, c.Description, c.IsActive, c.CreatedAt)).ToListAsync(cancellationToken);
    }

    public async Task<CategoryOperationResult> CreateAsync(CreateObservationCategoryDto dto, Guid actorId, CancellationToken cancellationToken = default)
    {
        var trimmedName = dto.Name.Trim();
        var exists = await _dbContext.ObservationCategories.AnyAsync(c => c.Name.ToLower() == trimmedName.ToLower(), cancellationToken);
        if (exists) return CategoryOperationResult.DuplicateName();

        var category = new ObservationCategory
        {
            Id = Guid.NewGuid(),
            Name = trimmedName,
            Description = NormalizeDescription(dto.Description),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.ObservationCategories.Add(category);

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = "Categoría de observación creada",
            AffectedEntity = $"ObservationCategory:{category.Id}",
            Date = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return CategoryOperationResult.Ok(new ObservationCategoryDto(category.Id, category.Name, category.Description, category.IsActive, category.CreatedAt));
    }

    public async Task<CategoryOperationResult> UpdateAsync(Guid id, UpdateObservationCategoryDto dto, Guid actorId, CancellationToken cancellationToken = default)
    {
        var category = await _dbContext.ObservationCategories.FindAsync([id], cancellationToken);
        if (category is null) return CategoryOperationResult.NotFound();

        var trimmedName = dto.Name.Trim();
        var exists = await _dbContext.ObservationCategories.AnyAsync(c => c.Id != id && c.Name.ToLower() == trimmedName.ToLower(), cancellationToken);
        if (exists) return CategoryOperationResult.DuplicateName();

        category.Name = trimmedName;
        category.Description = NormalizeDescription(dto.Description);

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = "Categoría de observación actualizada",
            AffectedEntity = $"ObservationCategory:{category.Id}",
            Date = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return CategoryOperationResult.Ok(new ObservationCategoryDto(category.Id, category.Name, category.Description, category.IsActive, category.CreatedAt));
    }

    public async Task<CategoryOperationResult> ToggleStatusAsync(Guid id, bool isActive, Guid actorId, CancellationToken cancellationToken = default)
    {
        var category = await _dbContext.ObservationCategories.FindAsync([id], cancellationToken);
        if (category is null) return CategoryOperationResult.NotFound();

        category.IsActive = isActive;

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = isActive ? "Categoría de observación activada" : "Categoría de observación desactivada",
            AffectedEntity = $"ObservationCategory:{category.Id}",
            Date = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return CategoryOperationResult.Ok();
    }

    // restringe el borrado fisico si la categoria ya tiene observaciones cargadas (SCRUM-177)
    public async Task<CategoryOperationResult> DeleteAsync(Guid id, Guid actorId, CancellationToken cancellationToken = default)
    {
        var category = await _dbContext.ObservationCategories.FindAsync([id], cancellationToken);
        if (category is null) return CategoryOperationResult.NotFound();

        var hasObservations = await _dbContext.Observations.AnyAsync(o => o.CategoryId == id, cancellationToken);
        if (hasObservations)
        {
            return CategoryOperationResult.HasObservations();
        }

        _dbContext.ObservationCategories.Remove(category);

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = "Categoría de observación eliminada",
            AffectedEntity = $"ObservationCategory:{id}",
            Date = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return CategoryOperationResult.Ok();
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description)) return null;
        return description.Trim();
    }
}