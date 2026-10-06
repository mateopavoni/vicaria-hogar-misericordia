namespace Vicaria.Application.SocialRecords;

public record SocialRecordSearchResultDto(
    Guid SocialRecordId,
    Guid PersonId,
    string FullName,
    string? Dni,
    DateTime LastModifiedAt);
