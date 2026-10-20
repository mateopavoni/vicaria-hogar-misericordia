using Vicaria.Domain.Entities;

namespace Vicaria.Application.MedicationAgendas;

public record MedicationAgendaItemDto(
    Guid Id,
    Guid PersonId,
    string PersonFullName,
    Guid MedicationId,
    string MedicationName,
    string Dose,
    MedicationIntakeStatus Status,
    string? Observation,
    DateTime? AdministeredAt,
    Guid? AdministeredByUserId
);

public record MedicationAgendaGroupDto(
    string Time,
    IReadOnlyList<MedicationAgendaItemDto> Intakes
);

public record DailyMedicationAgendaDto(
    DateTime Date,
    IReadOnlyList<MedicationAgendaGroupDto> Groups
);

public record UpdateMedicationIntakeStatusDto(
    MedicationIntakeStatus Status,
    string? Observation
);