using FluentValidation;

namespace Vicaria.Application.Auth;

public class ApproveUserDtoValidator : AbstractValidator<ApproveUserDto>
{
    public ApproveUserDtoValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("El rol a asignar es requerido.");
    }
}
