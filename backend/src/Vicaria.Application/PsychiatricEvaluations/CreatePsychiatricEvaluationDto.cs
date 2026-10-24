namespace Vicaria.Application.PsychiatricEvaluations;

// alta de una evaluación psiquiátrica (SCRUM-258): fecha, profesional y diagnóstico.
// La persona viaja en la ruta (POST /api/persons/{id}/psychiatric-evaluations), no en el body.
public record CreatePsychiatricEvaluationDto(
    DateTime Date,
    string? Professional,
    string? Diagnosis);
