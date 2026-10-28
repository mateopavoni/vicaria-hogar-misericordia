using FluentValidation;

namespace Vicaria.Application.CaritasReports;

public class GetCaritasReportQueryValidator : AbstractValidator<GetCaritasReportQueryDto>
{
    public GetCaritasReportQueryValidator()
    {
        RuleFor(x => x.Type)
            .Must(type => type == "trimestral" || type == "semestral")
            .WithMessage("El período debe ser trimestral o semestral.");

        RuleFor(x => x.To)
            .Must(to => to.Date <= DateTime.UtcNow.AddHours(-3).Date) // hoy de Córdoba (UTC-3)
            .WithMessage("La fecha de corte no puede ser futura.");
    }
}
