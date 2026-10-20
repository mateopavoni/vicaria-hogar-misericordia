namespace Vicaria.Application.MedicationHistories;

// una toma de un día pasado. Status toma los valores del contrato del front:
// "administered", "not-administered" o "unregistered" (estaba programada y nadie la marcó)
public record MedicationHistoryEntryDto(
    DateOnly Date,
    string Time,
    Guid SchemeId,
    Guid MedicationId,
    string MedicationName,
    string Dose,
    string Status,
    string? Observation,
    Guid? RecordedByUserId,
    string? RecordedByName,
    DateTime? RecordedAt
);
