using FluentValidation;

namespace Vicaria.Application.Persons;

public class UpdatePersonTypeDtoValidator : AbstractValidator<UpdatePersonTypeDto>
{
    public UpdatePersonTypeDtoValidator()
    {
        RuleFor(x => x.PersonType)
            .IsInEnum().WithMessage("El tipo de persona indicado no es válido.");
    }
}