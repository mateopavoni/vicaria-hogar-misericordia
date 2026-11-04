namespace Vicaria.Application.MedicationSchemes;

// alta de un esquema de medicación para una persona.
// La persona viaja en la ruta (POST /api/persons/{id}/medication-schemes),
// no en el body. Time usa el formato HH:mm.
public record CreateMedicationSchemeDto(
    Guid MedicationId,
    string Dose,
    string Time);
