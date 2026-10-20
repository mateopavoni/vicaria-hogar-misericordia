using FluentValidation;
using Vicaria.Application.Common;

namespace Vicaria.Application.Collaborators;

public class CreateCollaboratorDtoValidator : AbstractValidator<CreateCollaboratorDto>
{
    public CreateCollaboratorDtoValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede superar los 100 caracteres.")
            .MustBeAPlainName();

        RuleFor(x => x.LastName)
            .MaximumLength(100).WithMessage("El apellido no puede superar los 100 caracteres.")
            .MustBeAPlainName();

        RuleFor(x => x.Dni)
            .MaximumLength(20).WithMessage("El DNI no puede superar los 20 caracteres.");

        RuleFor(x => x.Phone)
            .MaximumLength(30).WithMessage("El teléfono no puede superar los 30 caracteres.");

        RuleFor(x => x.Email)
            .MaximumLength(255).WithMessage("El email no puede superar los 255 caracteres.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("El email no tiene un formato válido.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.WorkArea)
            .MaximumLength(100).WithMessage("El área de trabajo no puede superar los 100 caracteres.");
    }
}
