namespace Vicaria.Application.LifeStories;

// una entrada del historial de una etapa; Content es lo que se escribió en ese momento
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