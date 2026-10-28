using FluentValidation;
using Vicaria.Application.Common;

namespace Vicaria.Application.PsychiatricEvaluations;

public class CreatePsychiatricEvaluationDtoValidator : AbstractValidator<CreatePsychiatricEvaluationDto>
{
    public CreatePsychiatricEvaluationDtoValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty()
            .WithMessage("La fecha de la evaluación es obligatoria.")
            // "hoy" es el de Córdoba (UTC-3, sin horario de verano): entre las 21 y las 24 h la
            // fecha UTC ya es la del día siguiente
            .Must(date => date.Date <= DateTime.UtcNow.AddHours(-3).Date)
            .WithMessage("La fecha de la evaluación no puede ser futura.");

        RuleFor(x => x.Professional)
            .MaximumLength(200).WithMessage("El profesional no puede superar los 200 caracteres.");

        RuleFor(x => x.Diagnosis)
            .MustNotContainSensitiveContent();
    }
}
