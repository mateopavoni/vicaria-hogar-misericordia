using FluentValidation;
using Vicaria.Domain.Entities;

namespace Vicaria.Application.CasonaStays;

public class CasonaStayExitDtoValidator : AbstractValidator<CasonaStayExitDto>
{
    public CasonaStayExitDtoValidator()
    {
        When(x => x.ExitReason == StayExitReason.Other, () =>
        {
            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("El motivo es obligatorio cuando el motivo de egreso es Otro.")
                .MaximumLength(500).WithMessage("El motivo no puede superar los 500 caracteres.");
        });

        When(x => x.ExitReason != StayExitReason.Other, () =>
        {
            RuleFor(x => x.Reason)
                .MaximumLength(500).WithMessage("El motivo no puede superar los 500 caracteres.");
        });
        
        When(x => x.NewStatus.HasValue, () =>
        {
            RuleFor(x => x.NewStatus!.Value)
                .IsInEnum().WithMessage("El estado post-egreso especificado no es válido.");
        });
    }
}
