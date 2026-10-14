namespace Vicaria.Application.MedicationSchemes;

// forma de respuesta del esquema creado (SCRUM-232): el front (SCRUM-234) usa
// el objeto devuelto directamente, por eso viaja completo y no solo el id.
// Time en HH:mm y LoadedAt en ISO, igual que MedicationScheme del front.
public record MedicationSchemeDto(
    Guid Id,
    Guid PersonId,
    Guid MedicationId,
    string MedicationName,
    string Dose,
    string Time,
    DateTime LoadedAt,
    Guid LoadedByUserId,
    string LoadedByName,
    bool IsActive,
    bool PendingStock);
