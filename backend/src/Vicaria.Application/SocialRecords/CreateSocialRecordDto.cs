using Vicaria.Domain.Entities;

namespace Vicaria.Application.SocialRecords;

// solo FirstName es obligatorio (SCRUM-5); todo lo demás se puede completar después
public record CreateSocialRecordDto(
    string FirstName,
    string? LastName,
    string? Dni,
    DateTime? DateOfBirth,
    string? Phone,
    PersonType? PersonType,
    string? ReasonForEntry,
    DateTime? EntryDate,
    string? HousingSituation,
    string? OvernightLocation,
    string? Occupation,
    bool HasDocumentation,
    string? GeneralNotes,
    ContactDto? Contact);
