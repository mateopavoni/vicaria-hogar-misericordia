namespace Vicaria.Application.Observations;

public record CreateObservationDto(
    string Content,
    Guid? CategoryId
);

public record ObservationResponseDto(
    Guid Id,
    Guid PersonId,
    string Content,
    Guid? CategoryId,
    string? CategoryName,
    Guid AuthorUserId,
    string AuthorName,
    DateTime CreatedAt
);

public enum CreateObservationError
{
    PersonNotFound,
    CategoryNotFoundOrInactive
}

public record CreateObservationResult(
    bool Success,
    ObservationResponseDto? Data = null,
    CreateObservationError? Error = null,
    string? ErrorMessage = null
);
public record GetObservationsFilterDto(
    Guid? CategoryId = null,
    Guid? AuthorUserId = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null
);
public record ObservationsTimelineResponseDto(
    IReadOnlyList<ObservationResponseDto> Items,
    int TotalCount
);