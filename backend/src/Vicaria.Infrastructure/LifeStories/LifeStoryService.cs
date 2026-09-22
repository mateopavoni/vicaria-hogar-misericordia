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

    // edita una sola etapa de forma independiente (SCRUM-171): la persona se crea
    // la fila si no existe, y solo esa etapa actualiza su contenido y auditoría
    public async Task<LifeStoryResponseDto?> UpdateStageAsync(Guid personId, LifeStoryStage stage, string content, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var personExists = await _dbContext.People.AnyAsync(p => p.Id == personId, cancellationToken);
        if (!personExists) return null;

        var entity = await _dbContext.LifeStories
            .Include(l => l.BeforeHogarUpdatedByUser)
            .Include(l => l.InHogarUpdatedByUser)
            .Include(l => l.AfterHogarUpdatedByUser)
            .FirstOrDefaultAsync(l => l.PersonId == personId, cancellationToken);

        if (entity is null)
        {
            entity = new LifeStory { PersonId = personId };
            _dbContext.LifeStories.Add(entity);
        }

        ApplyStageContent(entity, stage, content, actorUserId, DateTime.UtcNow);

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Recargar entidades de navegación de usuario para reflejar nombres actualizados
        await _dbContext.Entry(entity).Reference(l => l.BeforeHogarUpdatedByUser).LoadAsync(cancellationToken);
        await _dbContext.Entry(entity).Reference(l => l.InHogarUpdatedByUser).LoadAsync(cancellationToken);
        await _dbContext.Entry(entity).Reference(l => l.AfterHogarUpdatedByUser).LoadAsync(cancellationToken);

        return MapToDto(entity);
    }

    // actualiza una etapa y su auditoría solo si el contenido cambió (consistente con SCRUM-172)
    private static void ApplyStageContent(LifeStory entity, LifeStoryStage stage, string content, Guid actorUserId, DateTime now)
    {
        var trimmed = content.Trim();

        switch (stage)
        {
            case LifeStoryStage.BeforeHogar:
                if (entity.BeforeHogar == trimmed) return;
                entity.BeforeHogar = trimmed;
                entity.BeforeHogarUpdatedByUserId = actorUserId;
                entity.BeforeHogarUpdatedAt = now;
                break;
            case LifeStoryStage.InHogar:
                if (entity.InHogar == trimmed) return;
                entity.InHogar = trimmed;
                entity.InHogarUpdatedByUserId = actorUserId;
                entity.InHogarUpdatedAt = now;
                break;
            case LifeStoryStage.AfterHogar:
                if (entity.AfterHogar == trimmed) return;
                entity.AfterHogar = trimmed;
                entity.AfterHogarUpdatedByUserId = actorUserId;
                entity.AfterHogarUpdatedAt = now;
                break;
        }
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