namespace Vicaria.Application.MedicationSchemes;

// alta de un esquema de medicación para una persona (SCRUM-232 / SCRUM-59).
// La persona viaja en la ruta (POST /api/persons/{id}/medication-schemes),
// no en el body. Time usa el formato del front (HH:mm, ver SCRUM-234).
public record CreateMedicationSchemeDto(
    Guid MedicationId,
    string Dose,
    string Time);
