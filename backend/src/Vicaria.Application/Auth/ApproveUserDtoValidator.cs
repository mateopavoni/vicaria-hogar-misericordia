using FluentValidation;

namespace Vicaria.Application.Auth;

public class ApproveUserDtoValidator : AbstractValidator<ApproveUserDto>
{
    public ApproveUserDtoValidator()
    {
        RuleFor(x => x.RolId)
            .NotEmpty().WithMessage("El rol a asignar es requerido.");
    }
}
