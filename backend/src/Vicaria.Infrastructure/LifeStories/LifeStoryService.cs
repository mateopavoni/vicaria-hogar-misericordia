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
            .Include(l => l.BeforeCentroBarrialUpdatedByUser)
            .Include(l => l.InCentroBarrialUpdatedByUser)
            .Include(l => l.AfterCentroBarrialUpdatedByUser)
            .FirstOrDefaultAsync(l => l.PersonId == personId, cancellationToken);

        var entriesByStage = await GetEntriesByStageAsync(personId, cancellationToken);

        if (entity is null)
        {
            return new LifeStoryResponseDto(
                Guid.Empty,
                personId,
                new LifeStorySectionDto(null, false, null, null, null, entriesByStage[LifeStoryStage.BeforeCentroBarrial]),
                new LifeStorySectionDto(null, false, null, null, null, entriesByStage[LifeStoryStage.InCentroBarrial]),
                new LifeStorySectionDto(null, false, null, null, null, entriesByStage[LifeStoryStage.AfterCentroBarrial])
            );
        }

        return MapToDto(entity, entriesByStage);
    }

    public async Task<LifeStoryResponseDto?> UpdateAsync(Guid personId, UpdateLifeStoryDto dto, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var personExists = await _dbContext.People.AnyAsync(p => p.Id == personId, cancellationToken);
        if (!personExists) return null;

        var entity = await _dbContext.LifeStories
            .Include(l => l.BeforeCentroBarrialUpdatedByUser)
            .Include(l => l.InCentroBarrialUpdatedByUser)
            .Include(l => l.AfterCentroBarrialUpdatedByUser)
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
        if (dto.BeforeCentroBarrial is not null && dto.BeforeCentroBarrial != entity.BeforeCentroBarrial)
        {
            ApplyStageContent(entity, LifeStoryStage.BeforeCentroBarrial, dto.BeforeCentroBarrial, actorUserId, now);
            AddEntry(personId, LifeStoryStage.BeforeCentroBarrial, dto.BeforeCentroBarrial, actorUserId, now);
        }

        if (dto.InCentroBarrial is not null && dto.InCentroBarrial != entity.InCentroBarrial)
        {
            ApplyStageContent(entity, LifeStoryStage.InCentroBarrial, dto.InCentroBarrial, actorUserId, now);
            AddEntry(personId, LifeStoryStage.InCentroBarrial, dto.InCentroBarrial, actorUserId, now);
        }

        if (dto.AfterCentroBarrial is not null && dto.AfterCentroBarrial != entity.AfterCentroBarrial)
        {
            ApplyStageContent(entity, LifeStoryStage.AfterCentroBarrial, dto.AfterCentroBarrial, actorUserId, now);
            AddEntry(personId, LifeStoryStage.AfterCentroBarrial, dto.AfterCentroBarrial, actorUserId, now);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Recargar entidades de navegación de usuario para reflejar nombres actualizados
        await _dbContext.Entry(entity).Reference(l => l.BeforeCentroBarrialUpdatedByUser).LoadAsync(cancellationToken);
        await _dbContext.Entry(entity).Reference(l => l.InCentroBarrialUpdatedByUser).LoadAsync(cancellationToken);
        await _dbContext.Entry(entity).Reference(l => l.AfterCentroBarrialUpdatedByUser).LoadAsync(cancellationToken);

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
            .Include(l => l.BeforeCentroBarrialUpdatedByUser)
            .Include(l => l.InCentroBarrialUpdatedByUser)
            .Include(l => l.AfterCentroBarrialUpdatedByUser)
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
        await _dbContext.Entry(entity).Reference(l => l.BeforeCentroBarrialUpdatedByUser).LoadAsync(cancellationToken);
        await _dbContext.Entry(entity).Reference(l => l.InCentroBarrialUpdatedByUser).LoadAsync(cancellationToken);
        await _dbContext.Entry(entity).Reference(l => l.AfterCentroBarrialUpdatedByUser).LoadAsync(cancellationToken);

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
            [LifeStoryStage.BeforeCentroBarrial] = [],
            [LifeStoryStage.InCentroBarrial] = [],
            [LifeStoryStage.AfterCentroBarrial] = []
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
            case LifeStoryStage.BeforeCentroBarrial:
                if (entity.BeforeCentroBarrial == trimmed) return;
                entity.BeforeCentroBarrial = trimmed;
                entity.BeforeCentroBarrialUpdatedByUserId = actorUserId;
                entity.BeforeCentroBarrialUpdatedAt = now;
                break;
            case LifeStoryStage.InCentroBarrial:
                if (entity.InCentroBarrial == trimmed) return;
                entity.InCentroBarrial = trimmed;
                entity.InCentroBarrialUpdatedByUserId = actorUserId;
                entity.InCentroBarrialUpdatedAt = now;
                break;
            case LifeStoryStage.AfterCentroBarrial:
                if (entity.AfterCentroBarrial == trimmed) return;
                entity.AfterCentroBarrial = trimmed;
                entity.AfterCentroBarrialUpdatedByUserId = actorUserId;
                entity.AfterCentroBarrialUpdatedAt = now;
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
                entity.BeforeCentroBarrial,
                !string.IsNullOrWhiteSpace(entity.BeforeCentroBarrial),
                entity.BeforeCentroBarrialUpdatedByUserId,
                FormatAuthor(entity.BeforeCentroBarrialUpdatedByUser),
                entity.BeforeCentroBarrialUpdatedAt,
                entriesByStage[LifeStoryStage.BeforeCentroBarrial]
            ),
            new LifeStorySectionDto(
                entity.InCentroBarrial,
                !string.IsNullOrWhiteSpace(entity.InCentroBarrial),
                entity.InCentroBarrialUpdatedByUserId,
                FormatAuthor(entity.InCentroBarrialUpdatedByUser),
                entity.InCentroBarrialUpdatedAt,
                entriesByStage[LifeStoryStage.InCentroBarrial]
            ),
            new LifeStorySectionDto(
                entity.AfterCentroBarrial,
                !string.IsNullOrWhiteSpace(entity.AfterCentroBarrial),
                entity.AfterCentroBarrialUpdatedByUserId,
                FormatAuthor(entity.AfterCentroBarrialUpdatedByUser),
                entity.AfterCentroBarrialUpdatedAt,
                entriesByStage[LifeStoryStage.AfterCentroBarrial]
            )
        );
    }
}
