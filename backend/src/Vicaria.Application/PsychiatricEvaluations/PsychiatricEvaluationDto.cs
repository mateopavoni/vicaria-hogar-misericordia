namespace Vicaria.Application.PsychiatricEvaluations;

// forma de respuesta del alta: viaja completa porque el front
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
