using Vicaria.Domain.Entities;

namespace Vicaria.Application.SocialRecords;

// perfil completo de una ficha, para la pantalla de detalle
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
    IReadOnlyList<PersonTypeHistoryItemDto> PersonTypeHistory,
    IReadOnlyList<StatusHistoryItemDto> StatusHistory,
    SocialRecordReportFieldsDto ReportFields);

// item del historial de cambios de tipo de persona
public record PersonTypeHistoryItemDto(
    Guid Id,
    string? PreviousValue,
    string NewValue,
    string ModifiedBy,
    DateTime ModifiedAt);

// item del historial de estados, con la forma del ChangeHistoryItem del
// frontend; ModifiedBy es "Sistema" en los pases automáticos del job
public record StatusHistoryItemDto(
    Guid Id,
    string PreviousValue,
    string NewValue,
    string ModifiedBy,
    DateTime ModifiedAt,
    string Reason);

// estadia en la Casa de Convivencia, tal como la consume el timeline del perfil
public record SocialRecordStayDto(
    Guid Id,
    DateTime EntryDate,
    DateTime? ExitDate,
    StayExitReason? ExitReason,
    int DurationInDays);
