using Microsoft.EntityFrameworkCore;
using Vicaria.Application.Activities;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.Infrastructure.Activities;

public class ActivityService : IActivityService
{
    private readonly VicariaDbContext _dbContext;

    public ActivityService(VicariaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ActivityDto>> ListAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Activities.AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(a => a.IsActive);
        }

        return await query
            .OrderBy(a => a.Name)
            .Select(a => new ActivityDto(a.Id, a.Name, a.IsActive, a.IsFromCalendar, a.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<GetActivityByIdResult> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var activity = await _dbContext.Activities
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (activity is null)
        {
            return GetActivityByIdResult.NotFound();
        }

        return GetActivityByIdResult.Success(new ActivityDto(
            activity.Id,
            activity.Name,
            activity.IsActive,
            activity.IsFromCalendar,
            activity.CreatedAt
        ));
    }

    public async Task<CreateActivityResult> CreateAsync(CreateActivityDto dto, Guid actorId, CancellationToken cancellationToken = default)
    {
        var normalizedName = dto.Name.Trim();

        var exists = await _dbContext.Activities
            .AnyAsync(a => a.IsActive && a.Name.ToLower() == normalizedName.ToLower(), cancellationToken);

        if (exists)
        {
            return CreateActivityResult.Duplicate();
        }

        var activity = new Activity
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
            IsActive = true,
            IsFromCalendar = false,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Activities.Add(activity);

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = $"Actividad creada: {activity.Name}",
            AffectedEntity = $"Activity:{activity.Id}",
            Date = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreateActivityResult.Success(activity.Id);
    }

    public async Task<UpdateActivityResult> UpdateAsync(Guid id, UpdateActivityDto dto, Guid actorId, CancellationToken cancellationToken = default)
    {
        var activity = await _dbContext.Activities
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (activity is null)
        {
            return UpdateActivityResult.NotFound();
        }

        var normalizedName = dto.Name.Trim();

        var duplicateExists = await _dbContext.Activities
            .AnyAsync(a => a.Id != id && a.IsActive && a.Name.ToLower() == normalizedName.ToLower(), cancellationToken);

        if (duplicateExists)
        {
            return UpdateActivityResult.Duplicate();
        }

        activity.Name = normalizedName;
        activity.IsActive = dto.IsActive;

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = $"Actividad modificada: {activity.Name} (Activa: {activity.IsActive})",
            AffectedEntity = $"Activity:{activity.Id}",
            Date = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return UpdateActivityResult.Success();
    }

    public async Task<DeleteActivityResult> DeleteAsync(Guid id, Guid actorId, CancellationToken cancellationToken = default)
    {
        var activity = await _dbContext.Activities
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (activity is null)
        {
            return DeleteActivityResult.NotFound();
        }

        activity.IsActive = false;

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = $"Actividad dada de baja: {activity.Name}",
            AffectedEntity = $"Activity:{activity.Id}",
            Date = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return DeleteActivityResult.Success();
    }
}