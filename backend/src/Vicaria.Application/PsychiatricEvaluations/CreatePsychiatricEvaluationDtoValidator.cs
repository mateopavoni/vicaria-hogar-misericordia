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
            .Must(date => date.Date <= DateTime.UtcNow.Date)
            .WithMessage("La fecha de la evaluación no puede ser futura.");

        RuleFor(x => x.Professional)
            .MaximumLength(200).WithMessage("El profesional no puede superar los 200 caracteres.");

        RuleFor(x => x.Diagnosis)
            .MustNotContainSensitiveContent();
    }
}
