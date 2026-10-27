using Microsoft.EntityFrameworkCore;
using Vicaria.Application.CaritasCatalogs;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.Infrastructure.CaritasCatalogs;

public class CaritasCatalogService : ICaritasCatalogService
{
    private readonly VicariaDbContext _dbContext;

    public CaritasCatalogService(VicariaDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<CaritasCatalogValueDto>> GetValuesAsync(CaritasCatalogCategory? category, bool onlyActive, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.CaritasCatalogValues.AsNoTracking();
        if (category.HasValue) query = query.Where(v => v.Category == category.Value);
        if (onlyActive) query = query.Where(v => v.IsActive);
        return await query.OrderBy(v => v.Name).Select(v => new CaritasCatalogValueDto(v.Id, v.Category, v.Name, v.Description, v.IsActive, v.CreatedAt)).ToListAsync(cancellationToken);
    }

    public async Task<CaritasCatalogOperationResult> CreateAsync(CreateCaritasCatalogValueDto dto, Guid actorId, CancellationToken cancellationToken = default)
    {
        var trimmedName = dto.Name.Trim();
        var exists = await _dbContext.CaritasCatalogValues.AnyAsync(v => v.Category == dto.Category && v.Name.ToLower() == trimmedName.ToLower(), cancellationToken);
        if (exists) return CaritasCatalogOperationResult.DuplicateName();

        var value = new CaritasCatalogValue
        {
            Id = Guid.NewGuid(),
            Category = dto.Category,
            Name = trimmedName,
            Description = NormalizeDescription(dto.Description),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.CaritasCatalogValues.Add(value);

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = "Valor de catálogo Caritas creado",
            AffectedEntity = $"CaritasCatalogValue:{value.Id}",
            Date = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return CaritasCatalogOperationResult.Ok(new CaritasCatalogValueDto(value.Id, value.Category, value.Name, value.Description, value.IsActive, value.CreatedAt));
    }

    public async Task<CaritasCatalogOperationResult> UpdateAsync(Guid id, UpdateCaritasCatalogValueDto dto, Guid actorId, CancellationToken cancellationToken = default)
    {
        var value = await _dbContext.CaritasCatalogValues.FindAsync([id], cancellationToken);
        if (value is null) return CaritasCatalogOperationResult.NotFound();

        var trimmedName = dto.Name.Trim();
        var exists = await _dbContext.CaritasCatalogValues.AnyAsync(v => v.Id != id && v.Category == value.Category && v.Name.ToLower() == trimmedName.ToLower(), cancellationToken);
        if (exists) return CaritasCatalogOperationResult.DuplicateName();

        value.Name = trimmedName;
        value.Description = NormalizeDescription(dto.Description);

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = "Valor de catálogo Caritas actualizado",
            AffectedEntity = $"CaritasCatalogValue:{value.Id}",
            Date = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return CaritasCatalogOperationResult.Ok(new CaritasCatalogValueDto(value.Id, value.Category, value.Name, value.Description, value.IsActive, value.CreatedAt));
    }

    public async Task<CaritasCatalogOperationResult> ToggleStatusAsync(Guid id, bool isActive, Guid actorId, CancellationToken cancellationToken = default)
    {
        var value = await _dbContext.CaritasCatalogValues.FindAsync([id], cancellationToken);
        if (value is null) return CaritasCatalogOperationResult.NotFound();

        value.IsActive = isActive;

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = isActive ? "Valor de catálogo Caritas activado" : "Valor de catálogo Caritas desactivado",
            AffectedEntity = $"CaritasCatalogValue:{value.Id}",
            Date = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return CaritasCatalogOperationResult.Ok();
    }

    // baja lógica (SCRUM-274): nunca se borra físicamente para conservar los datos históricos
    public async Task<CaritasCatalogOperationResult> DeleteAsync(Guid id, Guid actorId, CancellationToken cancellationToken = default)
    {
        var value = await _dbContext.CaritasCatalogValues.FindAsync([id], cancellationToken);
        if (value is null) return CaritasCatalogOperationResult.NotFound();

        value.IsActive = false;

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = "Valor de catálogo Caritas dado de baja",
            AffectedEntity = $"CaritasCatalogValue:{value.Id}",
            Date = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return CaritasCatalogOperationResult.Ok();
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description)) return null;
        return description.Trim();
    }
}
