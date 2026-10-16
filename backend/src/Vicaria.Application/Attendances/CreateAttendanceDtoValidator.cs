using FluentValidation;

namespace Vicaria.Application.Attendances;

public class CreateAttendanceDtoValidator : AbstractValidator<CreateAttendanceDto>
{
    public CreateAttendanceDtoValidator()
    {
        RuleFor(x => x.PersonId)
            .NotEmpty()
            .WithMessage("La persona es obligatoria para registrar la asistencia.");

        RuleFor(x => x.Activity)
            .IsInEnum()
            .WithMessage("La actividad no es válida.");

        RuleFor(x => x.Date)
            .NotEmpty()
            .WithMessage("La fecha es obligatoria para registrar la asistencia.")
            .Must(date => date.Date <= DateTime.UtcNow.Date)
            .WithMessage("La fecha no puede ser futura. Solo se puede registrar la asistencia del día actual o de días pasados.");

        RuleFor(x => x.AnonymousCount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("La cantidad de personas sin ficha no puede ser negativa.");
    }
}