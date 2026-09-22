using Microsoft.EntityFrameworkCore;
using Vicaria.Application.LifeStories;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.Infrastructure.LifeStories;

public class LifeStoryService : ILifeStoryService
{
    private readonly VicariaDbContext _dbContext;

    public LifeStoryService(VicariaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<LifeStoryResponseDto> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.LifeStories
            .AsNoTracking()
            .Include(l => l.BeforeHogarUpdatedByUser)
            .Include(l => l.InHogarUpdatedByUser)
            .Include(l => l.AfterHogarUpdatedByUser)
            .FirstOrDefaultAsync(l => l.PersonId == personId, cancellationToken);

        if (entity is null)
        {
            return new LifeStoryResponseDto(
                Guid.Empty,
                personId,
                new LifeStorySectionDto(null, false, null, null, null),
                new LifeStorySectionDto(null, false, null, null, null),
                new LifeStorySectionDto(null, false, null, null, null)
            );
        }

        return MapToDto(entity);
    }

    public async Task<LifeStoryResponseDto?> UpdateAsync(Guid personId, UpdateLifeStoryDto dto, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var personExists = await _dbContext.People.AnyAsync(p => p.Id == personId, cancellationToken);
        if (!personExists) return null;

        var entity = await _dbContext.LifeStories
            .Include(l => l.BeforeHogarUpdatedByUser)
            .Include(l => l.InHogarUpdatedByUser)
            .Include(l => l.AfterHogarUpdatedByUser)
            .FirstOrDefaultAsync(l => l.PersonId == personId, cancellationToken);

        var now = DateTime.UtcNow;

        if (entity is null)
        {
            entity = new LifeStory { PersonId = personId };
            _dbContext.LifeStories.Add(entity);
        }

        // Auditoría automática por etapa (SCRUM-172)
        if (dto.BeforeHogar is not null && dto.BeforeHogar != entity.BeforeHogar)
        {
            entity.BeforeHogar = dto.BeforeHogar.Trim();
            entity.BeforeHogarUpdatedByUserId = actorUserId;
            entity.BeforeHogarUpdatedAt = now;
        }

        if (dto.InHogar is not null && dto.InHogar != entity.InHogar)
        {
            entity.InHogar = dto.InHogar.Trim();
            entity.InHogarUpdatedByUserId = actorUserId;
            entity.InHogarUpdatedAt = now;
        }

        if (dto.AfterHogar is not null && dto.AfterHogar != entity.AfterHogar)
        {
            entity.AfterHogar = dto.AfterHogar.Trim();
            entity.AfterHogarUpdatedByUserId = actorUserId;
            entity.AfterHogarUpdatedAt = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Recargar entidades de navegación de usuario para reflejar nombres actualizados
        await _dbContext.Entry(entity).Reference(l => l.BeforeHogarUpdatedByUser).LoadAsync(cancellationToken);
        await _dbContext.Entry(entity).Reference(l => l.InHogarUpdatedByUser).LoadAsync(cancellationToken);
        await _dbContext.Entry(entity).Reference(l => l.AfterHogarUpdatedByUser).LoadAsync(cancellationToken);

        return MapToDto(entity);
    }

    private static LifeStoryResponseDto MapToDto(LifeStory entity)
    {
        static string? FormatAuthor(User? user) => user is null ? null : $"{user.FirstName} {user.LastName}".Trim();

        return new LifeStoryResponseDto(
            entity.Id,
            entity.PersonId,
            new LifeStorySectionDto(
                entity.BeforeHogar,
                !string.IsNullOrWhiteSpace(entity.BeforeHogar),
                entity.BeforeHogarUpdatedByUserId,
                FormatAuthor(entity.BeforeHogarUpdatedByUser),
                entity.BeforeHogarUpdatedAt
            ),
            new LifeStorySectionDto(
                entity.InHogar,
                !string.IsNullOrWhiteSpace(entity.InHogar),
                entity.InHogarUpdatedByUserId,
                FormatAuthor(entity.InHogarUpdatedByUser),
                entity.InHogarUpdatedAt
            ),
            new LifeStorySectionDto(
                entity.AfterHogar,
                !string.IsNullOrWhiteSpace(entity.AfterHogar),
                entity.AfterHogarUpdatedByUserId,
                FormatAuthor(entity.AfterHogarUpdatedByUser),
                entity.AfterHogarUpdatedAt
            )
        );
    }
}