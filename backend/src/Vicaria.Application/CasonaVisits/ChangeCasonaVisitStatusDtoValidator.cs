using FluentValidation;

namespace Vicaria.Application.CasonaVisits;

public class ChangeCasonaVisitStatusDtoValidator : AbstractValidator<ChangeCasonaVisitStatusDto>
{
    public ChangeCasonaVisitStatusDtoValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("El estado especificado no es válido.");

        RuleFor(x => x.CancellationReason)
            .MaximumLength(500).WithMessage("El motivo de cancelación no puede superar los 500 caracteres.");
    }
}