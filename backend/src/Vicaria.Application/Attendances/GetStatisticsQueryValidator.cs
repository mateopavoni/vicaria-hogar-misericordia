using FluentValidation;
using Vicaria.Domain.Entities;

namespace Vicaria.Application.Attendances;

public class GetStatisticsQueryValidator : AbstractValidator<GetStatisticsQueryDto>
{
    public GetStatisticsQueryValidator()
    {
        RuleFor(x => x.From)
            .LessThanOrEqualTo(x => x.To)
            .WithMessage("La fecha desde no puede ser posterior a la fecha hasta.");

        RuleFor(x => x)
            .Must(x => (x.To.Date - x.From.Date).TotalDays <= 366)
            .WithMessage("El rango no puede superar los 366 días.");

        When(x => !string.IsNullOrWhiteSpace(x.Activity), () =>
        {
            RuleFor(x => x.Activity)
                .Must(activity => Enum.TryParse<AttendanceActivity>(activity, out _))
                .WithMessage("La actividad no es válida.");
        });
    }
}
