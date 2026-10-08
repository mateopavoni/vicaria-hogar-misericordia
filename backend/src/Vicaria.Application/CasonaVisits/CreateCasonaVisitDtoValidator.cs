using FluentValidation;

namespace Vicaria.Application.CasonaVisits;

public class CreateCasonaVisitDtoValidator : AbstractValidator<CreateCasonaVisitDto>
{
    public CreateCasonaVisitDtoValidator()
    {
        RuleFor(x => x.PersonId)
            .NotEmpty().WithMessage("Debe indicar la persona que recibe la visita.");

        RuleFor(x => x.VisitorName)
            .NotEmpty().WithMessage("El nombre del visitante es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre del visitante no puede superar los 100 caracteres.");

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("La fecha de la visita es obligatoria.")
            .Must((dto, date) => date.Date + dto.StartTime >= DateTime.UtcNow)
            .WithMessage("La fecha y hora de la visita no puede ser en el pasado.");

        RuleFor(x => x.EstimatedDurationMinutes)
            .GreaterThan(0).WithMessage("La duración estimada debe ser mayor a 0 minutos.");
    }
}
