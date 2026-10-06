namespace Vicaria.Application.LifeStories;

// una entrada histórica de una etapa (bug reportado 2026-09-23: guardar pisaba el contenido
// anterior en vez de sumar una entrada nueva) — Content es lo que se escribió en ese momento.
public record LifeStoryEntryDto(
    Guid Id,
    string Content,
    Guid CreatedByUserId,
    string? CreatedByName,
    DateTime CreatedAt
);

public record LifeStorySectionDto(
    string? Content,
    bool IsCompleted,
    Guid? UpdatedByUserId,
    string? UpdatedByName,
    DateTime? UpdatedAt,
    IReadOnlyList<LifeStoryEntryDto> Entries
);

public record LifeStoryResponseDto(
    Guid Id,
    Guid PersonId,
    LifeStorySectionDto BeforeCentroBarrial,
    LifeStorySectionDto InCentroBarrial,
    LifeStorySectionDto AfterCentroBarrial
);

public record UpdateLifeStoryDto(
    string? BeforeCentroBarrial = null,
    string? InCentroBarrial = null,
    string? AfterCentroBarrial = null
);