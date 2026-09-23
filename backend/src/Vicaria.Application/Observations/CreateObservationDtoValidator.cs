using FluentValidation;

namespace Vicaria.Application.Observations;

public class CreateObservationDtoValidator : AbstractValidator<CreateObservationDto>
{
    public CreateObservationDtoValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("El texto de la observación es obligatorio.")
            .MaximumLength(4000).WithMessage("La observación no puede superar los 4000 caracteres.");

        When(x => x.CategoryId.HasValue, () =>
        {
            RuleFor(x => x.CategoryId!.Value)
                .NotEmpty().WithMessage("El identificador de categoría no es válido.");
        });
    }
}