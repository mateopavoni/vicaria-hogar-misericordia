namespace Vicaria.Application.MedicationHistories;

// datos para generar el PDF del historial: el mismo conjunto que
// el JSON más el encabezado para presentar a un profesional de salud
public record MedicationHistoryPdfDocument(
    string PersonName,
    string? PersonDni,
    DateOnly? From,
    DateOnly? To,
    IReadOnlyList<MedicationHistoryEntryDto> Entries,
    DateTime GeneratedAt
);
