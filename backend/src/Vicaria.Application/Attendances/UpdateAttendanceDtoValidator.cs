using FluentValidation;

namespace Vicaria.Application.Attendances;

public class UpdateAttendanceDtoValidator : AbstractValidator<UpdateAttendanceDto>
{
    public UpdateAttendanceDtoValidator()
    {
        // la obligatoriedad de la justificación depende de si el día está cerrado
        // (requiere leer la asistencia en BD), así que acá solo se valida formato
        RuleFor(x => x.AnonymousCount)
            .GreaterThanOrEqualTo(0)
            .When(x => x.AnonymousCount.HasValue)
            .WithMessage("La cantidad de personas sin ficha no puede ser negativa.");

        RuleFor(x => x.Justification)
            .MaximumLength(500)
            .When(x => x.Justification is not null)
            .WithMessage("La justificación no puede superar los 500 caracteres.");
    }
}
