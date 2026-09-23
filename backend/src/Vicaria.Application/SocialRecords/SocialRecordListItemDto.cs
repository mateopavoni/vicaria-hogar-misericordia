using Vicaria.Domain.Entities;

namespace Vicaria.Application.SocialRecords;

public record SocialRecordListItemDto(
    Guid Id,
    Guid PersonId,
    string FirstName,
    string? LastName,
    string? Dni,
    DateTime? DateOfBirth,
    PersonType? PersonType,
    SocialRecordStatus Status,
    DateTime LastModifiedAt);
