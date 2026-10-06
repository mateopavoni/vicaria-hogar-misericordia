using Vicaria.Domain.Entities;

namespace Vicaria.Application.SocialRecords;

// perfil completo de una ficha, para la pantalla de detalle (SCRUM-8/121)
public record SocialRecordDetailDto(
    Guid Id,
    Guid PersonId,
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
    string? GeneralNotes,
    bool HasDocumentation,
    ContactDto? Contact,
    SocialRecordStatus Status,
    DateTime LastModifiedAt,
    IReadOnlyList<SocialRecordStayDto> StaysHistory,
    IReadOnlyList<PersonTypeHistoryItemDto> PersonTypeHistory);

// item de historial de cambio de tipo de persona, con la misma forma que espera
// el ChangeHistoryItem del frontend (bug reportado 2026-09-23: nunca existió este campo)
public record PersonTypeHistoryItemDto(
    Guid Id,
    string? PreviousValue,
    string NewValue,
    string ModifiedBy,
    DateTime ModifiedAt);

// estadia en la Casa de Convivencia, tal como la consume el timeline del perfil
public record SocialRecordStayDto(
    Guid Id,
    DateTime EntryDate,
    DateTime? ExitDate,
    StayExitReason? ExitReason,
    int DurationInDays);
