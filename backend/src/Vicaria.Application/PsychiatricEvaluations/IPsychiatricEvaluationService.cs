namespace Vicaria.Application.PsychiatricEvaluations;

public interface IPsychiatricEvaluationService
{
    // alta con unicidad de vigente (SCRUM-258): invalida las anteriores de la persona,
    // actualiza HasPsychiatricEvaluation en la ficha y audita, todo en una transacción
    Task<CreatePsychiatricEvaluationResult> CreateAsync(Guid personId, CreatePsychiatricEvaluationDto dto, Guid actorId, CancellationToken cancellationToken = default);
}
