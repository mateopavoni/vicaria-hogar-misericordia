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

        var entriesByStage = await GetEntriesByStageAsync(personId, cancellationToken);

        if (entity is null)
        {
            return new LifeStoryResponseDto(
                Guid.Empty,
                personId,
                new LifeStorySectionDto(null, false, null, null, null, entriesByStage[LifeStoryStage.BeforeHogar]),
                new LifeStorySectionDto(null, false, null, null, null, entriesByStage[LifeStoryStage.InHogar]),
                new LifeStorySectionDto(null, false, null, null, null, entriesByStage[LifeStoryStage.AfterHogar])
            );
        }

        return MapToDto(entity, entriesByStage);
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

        // Auditoría automática por etapa (SCRUM-172) — este endpoint (PUT masivo de las 3
        // etapas) también suma una entrada nueva al historial de cada etapa que cambió,
        // igual que UpdateStageAsync, para no dejar un camino que siga pisando contenido.
        if (dto.BeforeHogar is not null && dto.BeforeHogar != entity.BeforeHogar)
        {
            ApplyStageContent(entity, LifeStoryStage.BeforeHogar, dto.BeforeHogar, actorUserId, now);
            AddEntry(personId, LifeStoryStage.BeforeHogar, dto.BeforeHogar, actorUserId, now);
        }

        if (dto.InHogar is not null && dto.InHogar != entity.InHogar)
        {
            ApplyStageContent(entity, LifeStoryStage.InHogar, dto.InHogar, actorUserId, now);
            AddEntry(personId, LifeStoryStage.InHogar, dto.InHogar, actorUserId, now);
        }

        if (dto.AfterHogar is not null && dto.AfterHogar != entity.AfterHogar)
        {
            ApplyStageContent(entity, LifeStoryStage.AfterHogar, dto.AfterHogar, actorUserId, now);
            AddEntry(personId, LifeStoryStage.AfterHogar, dto.AfterHogar, actorUserId, now);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Recargar entidades de navegación de usuario para reflejar nombres actualizados
        await _dbContext.Entry(entity).Reference(l => l.BeforeHogarUpdatedByUser).LoadAsync(cancellationToken);
        await _dbContext.Entry(entity).Reference(l => l.InHogarUpdatedByUser).LoadAsync(cancellationToken);
        await _dbContext.Entry(entity).Reference(l => l.AfterHogarUpdatedByUser).LoadAsync(cancellationToken);

        var entriesByStage = await GetEntriesByStageAsync(personId, cancellationToken);
        return MapToDto(entity, entriesByStage);
    }

    // guarda una nueva entrada de una etapa (bug reportado 2026-09-23: antes pisaba el
    // contenido anterior en vez de crear una entrada nueva). LifeStory sigue actualizándose
    // como caché de "última entrada" para no romper a nadie que solo lea el valor actual;
    // la fuente de verdad del historial completo es LifeStoryEntry (append-only).
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

        var now = DateTime.UtcNow;
        var trimmed = content.Trim();

        if (!string.IsNullOrWhiteSpace(trimmed))
        {
            AddEntry(personId, stage, trimmed, actorUserId, now);
        }

        ApplyStageContent(entity, stage, content, actorUserId, now);

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Recargar entidades de navegación de usuario para reflejar nombres actualizados
        await _dbContext.Entry(entity).Reference(l => l.BeforeHogarUpdatedByUser).LoadAsync(cancellationToken);
        await _dbContext.Entry(entity).Reference(l => l.InHogarUpdatedByUser).LoadAsync(cancellationToken);
        await _dbContext.Entry(entity).Reference(l => l.AfterHogarUpdatedByUser).LoadAsync(cancellationToken);

        var entriesByStage = await GetEntriesByStageAsync(personId, cancellationToken);
        return MapToDto(entity, entriesByStage);
    }

    private void AddEntry(Guid personId, LifeStoryStage stage, string content, Guid actorUserId, DateTime now)
    {
        _dbContext.LifeStoryEntries.Add(new LifeStoryEntry
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            Stage = stage,
            Content = content.Trim(),
            CreatedByUserId = actorUserId,
            CreatedAt = now
        });
    }

    private async Task<Dictionary<LifeStoryStage, IReadOnlyList<LifeStoryEntryDto>>> GetEntriesByStageAsync(Guid personId, CancellationToken cancellationToken)
    {
        var entries = await _dbContext.LifeStoryEntries
            .AsNoTracking()
            .Include(e => e.CreatedByUser)
            .Where(e => e.PersonId == personId)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new
            {
                e.Id,
                e.Stage,
                e.Content,
                e.CreatedByUserId,
                AuthorFirstName = e.CreatedByUser.FirstName,
                AuthorLastName = e.CreatedByUser.LastName,
                e.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var result = new Dictionary<LifeStoryStage, IReadOnlyList<LifeStoryEntryDto>>
        {
            [LifeStoryStage.BeforeHogar] = [],
            [LifeStoryStage.InHogar] = [],
            [LifeStoryStage.AfterHogar] = []
        };

        foreach (var group in entries.GroupBy(e => e.Stage))
        {
            result[group.Key] = group
                .Select(e => new LifeStoryEntryDto(
                    e.Id,
                    e.Content,
                    e.CreatedByUserId,
                    $"{e.AuthorFirstName} {e.AuthorLastName}".Trim(),
                    e.CreatedAt))
                .ToList();
        }

        return result;
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

    private static LifeStoryResponseDto MapToDto(LifeStory entity, Dictionary<LifeStoryStage, IReadOnlyList<LifeStoryEntryDto>> entriesByStage)
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
                entity.BeforeHogarUpdatedAt,
                entriesByStage[LifeStoryStage.BeforeHogar]
            ),
            new LifeStorySectionDto(
                entity.InHogar,
                !string.IsNullOrWhiteSpace(entity.InHogar),
                entity.InHogarUpdatedByUserId,
                FormatAuthor(entity.InHogarUpdatedByUser),
                entity.InHogarUpdatedAt,
                entriesByStage[LifeStoryStage.InHogar]
            ),
            new LifeStorySectionDto(
                entity.AfterHogar,
                !string.IsNullOrWhiteSpace(entity.AfterHogar),
                entity.AfterHogarUpdatedByUserId,
                FormatAuthor(entity.AfterHogarUpdatedByUser),
                entity.AfterHogarUpdatedAt,
                entriesByStage[LifeStoryStage.AfterHogar]
            )
        );
    }
}
