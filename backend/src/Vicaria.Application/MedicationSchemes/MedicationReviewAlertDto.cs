namespace Vicaria.Application.MedicationSchemes;

// fila de la consulta de esquemas vencidos (SCRUM-249): un esquema vigente que
// lleva más de 30 días sin revisión. DaysWithoutReview se calcula desde
// ReviewedAt o, si nunca se revisó, desde LoadedAt.
public record MedicationReviewAlertDto(
    Guid SchemeId,
    Guid PersonId,
    string PersonName,
    Guid MedicationId,
    string MedicationName,
    string Dose,
    string Time,
    DateTime? ReviewedAt,
    DateTime LoadedAt,
    int DaysWithoutReview);
