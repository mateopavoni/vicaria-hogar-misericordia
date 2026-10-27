namespace Vicaria.Application.PsychiatricEvaluations;

public record PsychiatricEvaluationStatusDto(
    bool HasEvaluation,
    Guid? EvaluationId,
    DateTime? Date,
    DateTime? ExpiresAt,
    int? DiasParaVencer,
    bool EstaVencida,
    bool AlertaVencimientoProximo,
    string? Professional,
    string? Diagnosis
);

public record PsychiatricEvaluationHistoryItemDto(
    Guid Id,
    Guid PersonId,
    DateTime Date,
    DateTime ExpiresAt,
    string? Professional,
    string? Diagnosis,
    bool IsValid,
    Guid RegisteredByUserId,
    string RegisteredByName
);