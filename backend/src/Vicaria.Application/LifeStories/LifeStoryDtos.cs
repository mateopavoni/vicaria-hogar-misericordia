namespace Vicaria.Application.LifeStories;

public record LifeStorySectionDto(
    string? Content,
    bool IsCompleted,
    Guid? UpdatedByUserId,
    string? UpdatedByName,
    DateTime? UpdatedAt
);

public record LifeStoryResponseDto(
    Guid Id,
    Guid PersonId,
    LifeStorySectionDto BeforeHogar,
    LifeStorySectionDto InHogar,
    LifeStorySectionDto AfterHogar
);

public record UpdateLifeStoryDto(
    string? BeforeHogar = null,
    string? InHogar = null,
    string? AfterHogar = null
);