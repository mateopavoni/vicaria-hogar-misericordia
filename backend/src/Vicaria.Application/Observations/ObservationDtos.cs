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