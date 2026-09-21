using FluentValidation;

namespace Vicaria.Application.Observations;

public class CreateObservationCategoryDtoValidator : AbstractValidator<CreateObservationCategoryDto>
{
    public CreateObservationCategoryDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("El nombre de la categoría es obligatorio.").MaximumLength(100).WithMessage("El nombre no puede superar los 100 caracteres.");
    }
}

public class UpdateObservationCategoryDtoValidator : AbstractValidator<UpdateObservationCategoryDto>
{
    public UpdateObservationCategoryDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("El nombre de la categoría es obligatorio.").MaximumLength(100).WithMessage("El nombre no puede superar los 100 caracteres.");
    }
}