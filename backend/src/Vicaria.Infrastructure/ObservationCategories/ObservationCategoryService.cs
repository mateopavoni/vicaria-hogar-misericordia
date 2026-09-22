using Microsoft.EntityFrameworkCore;
using Vicaria.Application.ObservationCategories;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.Infrastructure.ObservationCategories;

public class ObservationCategoryService : IObservationCategoryService
{
    private readonly VicariaDbContext _dbContext;

    public ObservationCategoryService(VicariaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CreateObservationCategoryResult> CreateAsync(CreateObservationCategoryDto dto, Guid actorId, CancellationToken cancellationToken = default)
    {
        var category = new ObservationCategory
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
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

        return CreateObservationCategoryResult.Ok(category.Id);
    }

    public async Task<UpdateObservationCategoryResult> UpdateAsync(Guid categoryId, UpdateObservationCategoryDto dto, Guid actorId, CancellationToken cancellationToken = default)
    {
        var category = await _dbContext.ObservationCategories
            .FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);

        if (category is null)
        {
            return UpdateObservationCategoryResult.CategoryNotFound();
        }

        category.Name = dto.Name.Trim();
        category.Description = dto.Description?.Trim();

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = "Categoría de observación actualizada",
            AffectedEntity = $"ObservationCategory:{category.Id}",
            Date = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return UpdateObservationCategoryResult.Ok();
    }

    public async Task<DeactivateObservationCategoryResult> DeactivateAsync(Guid categoryId, Guid actorId, CancellationToken cancellationToken = default)
    {
        var category = await _dbContext.ObservationCategories
            .FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);

        if (category is null)
        {
            return DeactivateObservationCategoryResult.CategoryNotFound();
        }

        if (!category.IsActive)
        {
            return DeactivateObservationCategoryResult.AlreadyDeactivated();
        }

        // soft delete (SCRUM-176): nunca se borra físicamente, solo se desactiva
        category.IsActive = false;

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = "Categoría de observación desactivada",
            AffectedEntity = $"ObservationCategory:{category.Id}",
            Date = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return DeactivateObservationCategoryResult.Ok();
    }
}