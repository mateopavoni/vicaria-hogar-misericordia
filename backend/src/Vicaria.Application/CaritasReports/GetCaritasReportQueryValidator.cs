using FluentValidation;
using Vicaria.Application.Common;

namespace Vicaria.Application.CaritasReports;

public class GetCaritasReportQueryValidator : AbstractValidator<GetCaritasReportQueryDto>
{
    public GetCaritasReportQueryValidator()
    {
        RuleFor(x => x.Type)
            .Must(type => type == "trimestral" || type == "semestral")
            .WithMessage("El período debe ser trimestral o semestral.");

        RuleFor(x => x.To)
            .Must(to => to.Date <= CordobaClock.Today)
            .WithMessage("La fecha de corte no puede ser futura.");
    }
}
