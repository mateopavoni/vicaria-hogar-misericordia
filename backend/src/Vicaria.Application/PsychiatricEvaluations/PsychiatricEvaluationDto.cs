namespace Vicaria.Application.PsychiatricEvaluations;

// forma de respuesta del alta (SCRUM-258): viaja completa porque el front (SCRUM-260)
// muestra la vigente y calcula la alerta con el vencimiento. ExpiresAt = Date + 6 meses.
public record PsychiatricEvaluationDto(
    Guid Id,
    Guid PersonId,
    DateTime Date,
    string? Professional,
    string? Diagnosis,
    bool IsValid,
    DateTime ExpiresAt,
    Guid RegisteredByUserId);
