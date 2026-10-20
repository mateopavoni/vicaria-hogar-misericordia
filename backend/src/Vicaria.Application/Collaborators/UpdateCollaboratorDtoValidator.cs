using FluentValidation;

namespace Vicaria.Application.Collaborators;

public class UpdateCollaboratorDtoValidator : AbstractValidator<UpdateCollaboratorDto>
{
    public UpdateCollaboratorDtoValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede superar los 100 caracteres.");

        RuleFor(x => x.LastName)
            .MaximumLength(100).WithMessage("El apellido no puede superar los 100 caracteres.");

        RuleFor(x => x.Dni)
            .MaximumLength(20).WithMessage("El DNI no puede superar los 20 caracteres.");

        RuleFor(x => x.Phone)
            .MaximumLength(30).WithMessage("El teléfono no puede superar los 30 caracteres.");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("El formato del email no es válido.")
            .MaximumLength(255).WithMessage("El email no puede superar los 255 caracteres.");

        RuleFor(x => x.WorkArea)
            .MaximumLength(100).WithMessage("El área de trabajo no puede superar los 100 caracteres.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("El tipo de colaborador no es válido.");
    }
}