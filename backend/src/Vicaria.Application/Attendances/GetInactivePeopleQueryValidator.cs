using FluentValidation;

namespace Vicaria.Application.Attendances;

public class GetInactivePeopleQueryValidator : AbstractValidator<GetInactivePeopleQueryDto>
{
    public GetInactivePeopleQueryValidator()
    {
        RuleFor(x => x.Days)
            .InclusiveBetween(1, 365)
            .WithMessage("La cantidad de días debe estar entre 1 y 365.");
    }
}
