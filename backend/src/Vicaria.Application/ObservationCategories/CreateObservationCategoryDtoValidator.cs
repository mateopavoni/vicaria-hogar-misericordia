using FluentValidation;

namespace Vicaria.Application.ObservationCategories;

public class CreateObservationCategoryDtoValidator : AbstractValidator<CreateObservationCategoryDto>
{
    public CreateObservationCategoryDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre de la categoría es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre de la categoría no puede superar los 100 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("La descripción de la categoría no puede superar los 500 caracteres.");
    }
}