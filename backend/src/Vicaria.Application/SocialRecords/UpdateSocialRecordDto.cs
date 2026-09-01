using Vicaria.Domain.Entities;

namespace Vicaria.Application.SocialRecords;

// mismos campos editables que la creación (SCRUM-7); Contact no se edita acá
public record UpdateSocialRecordDto(
    string FirstName,
    string? LastName,
    string? Dni,
    DateTime? DateOfBirth,
    PersonType? PersonType,
    string? ReasonForEntry,
    DateTime? EntryDate,
    string? HousingSituation,
    string? OvernightLocation,
    string? Occupation,
    bool HasDocumentation,
    string? GeneralNotes);
