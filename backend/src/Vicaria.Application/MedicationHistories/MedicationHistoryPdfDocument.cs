namespace Vicaria.Application.MedicationHistories;

// datos para generar el PDF del historial (SCRUM-241): el mismo conjunto que
// el JSON (SCRUM-240) más el encabezado para presentar a un profesional de salud
public record MedicationHistoryPdfDocument(
    string PersonName,
    string? PersonDni,
    DateOnly? From,
    DateOnly? To,
    IReadOnlyList<MedicationHistoryEntryDto> Entries,
    DateTime GeneratedAt
);
