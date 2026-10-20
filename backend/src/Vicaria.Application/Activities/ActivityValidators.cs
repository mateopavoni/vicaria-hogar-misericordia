using FluentValidation;

namespace Vicaria.Application.Activities;

public class CreateActivityValidator : AbstractValidator<CreateActivityDto>
{
    public CreateActivityValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre de la actividad es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre de la actividad no puede superar los 100 caracteres.");
    }
}

public class UpdateActivityValidator : AbstractValidator<UpdateActivityDto>
{
    public UpdateActivityValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre de la actividad es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre de la actividad no puede superar los 100 caracteres.");
    }
}