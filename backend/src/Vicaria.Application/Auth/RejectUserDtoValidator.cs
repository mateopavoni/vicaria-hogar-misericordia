using FluentValidation;

namespace Vicaria.Application.Auth;

public class RejectUserDtoValidator : AbstractValidator<RejectUserDto>
{
    public RejectUserDtoValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("El motivo del rechazo es requerido.")
            .MaximumLength(500);
    }
}
