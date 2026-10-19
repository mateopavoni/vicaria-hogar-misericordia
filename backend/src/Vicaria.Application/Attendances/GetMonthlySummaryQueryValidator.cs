using FluentValidation;

namespace Vicaria.Application.Attendances;

public class GetMonthlySummaryQueryValidator : AbstractValidator<GetMonthlySummaryQueryDto>
{
    public GetMonthlySummaryQueryValidator()
    {
        RuleFor(x => x.Year)
            .InclusiveBetween(2000, 2100)
            .WithMessage("El año debe estar entre 2000 y 2100.");

        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12)
            .WithMessage("El mes debe estar entre 1 y 12.");
    }
}
