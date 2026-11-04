namespace Vicaria.Application.MedicationHistories;

// filtros del historial de tomas: rango inclusivo de fechas y medicamento.
// todos opcionales; sin valor = sin límite
public record GetMedicationHistoryDto(
    DateOnly? From,
    DateOnly? To,
    Guid? MedicationId
);
