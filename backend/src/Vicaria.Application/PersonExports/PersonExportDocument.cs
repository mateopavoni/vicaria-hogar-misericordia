namespace Vicaria.Application.PersonExports;

// item de observación para el PDF (SCRUM-252): fecha, categoría y contenido;
// no lleva autor (decisión: excluir datos de terceros/otros usuarios)
public record PersonExportObservationItem(
    DateTime CreatedAt,
    string? CategoryName,
    string Content
);

// documento de exportación de la ficha (SCRUM-252): mismo contenido que el
// perfil según las secciones (datos / datos-observaciones / todo) más el
// encabezado (sistema, fecha y referente que generó). No lleva el contacto de referencia:
// es un tercero (criterio de SCRUM-23)
public record PersonExportDocument(
    Guid PersonId,
    string FirstName,
    string? LastName,
    string? Dni,
    DateTime? DateOfBirth,
    string? Phone,
    string? PersonType,
    string? ReasonForEntry,
    DateTime? EntryDate,
    string? HousingSituation,
    string? OvernightLocation,
    string? Occupation,
    bool HasDocumentation,
    string? GeneralNotes,
    string Sections,
    IReadOnlyList<PersonExportObservationItem> Observations,
    string? BeforeCentroBarrial,
    string? InCentroBarrial,
    string? AfterCentroBarrial,
    string GeneratedByName,
    string GeneratedByRole,
    DateTime GeneratedAtUtc
);
