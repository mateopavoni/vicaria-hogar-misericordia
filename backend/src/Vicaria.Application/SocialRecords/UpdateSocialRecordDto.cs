using Vicaria.Domain.Entities;

namespace Vicaria.Application.SocialRecords;

// mismos campos editables que la creación, incluido el contacto de referencia
public record UpdateSocialRecordDto(
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
