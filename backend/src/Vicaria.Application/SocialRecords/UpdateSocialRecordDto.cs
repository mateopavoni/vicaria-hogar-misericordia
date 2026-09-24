using Vicaria.Domain.Entities;

namespace Vicaria.Application.SocialRecords;

// mismos campos editables que la creación (SCRUM-7). Contact se agrega acá porque el
// formulario de edición del frontend ya lo enviaba y se perdía en silencio (bug reportado
// 2026-09-23: UpdateAsync no tenía este campo, a diferencia de CreateAsync).
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
