using FluentValidation;

namespace Vicaria.Application.Persons;

public class UpdatePersonProfileStatusValidator : AbstractValidator<UpdatePersonProfileStatusDto>
{
    public UpdatePersonProfileStatusValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("El estado especificado no es válido.");
    }
}