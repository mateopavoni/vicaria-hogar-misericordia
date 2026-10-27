using FluentValidation;

namespace Vicaria.Application.CaritasCatalogs;

public class CreateCaritasCatalogValueDtoValidator : AbstractValidator<CreateCaritasCatalogValueDto>
{
    public CreateCaritasCatalogValueDtoValidator()
    {
        RuleFor(x => x.Category).IsInEnum().WithMessage("La categoría del valor no es válida.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("El nombre del valor es obligatorio.").MaximumLength(100).WithMessage("El nombre no puede superar los 100 caracteres.");
        RuleFor(x => x.Description).MaximumLength(500).WithMessage("La descripción no puede superar los 500 caracteres.");
    }
}

public class UpdateCaritasCatalogValueDtoValidator : AbstractValidator<UpdateCaritasCatalogValueDto>
{
    public UpdateCaritasCatalogValueDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("El nombre del valor es obligatorio.").MaximumLength(100).WithMessage("El nombre no puede superar los 100 caracteres.");
        RuleFor(x => x.Description).MaximumLength(500).WithMessage("La descripción no puede superar los 500 caracteres.");
    }
}
