using FluentValidation;

namespace Vicaria.Application.Attendances;

public class CreateAttendanceDtoValidator : AbstractValidator<CreateAttendanceDto>
{
    public CreateAttendanceDtoValidator()
    {
        RuleFor(x => x.PersonId)
            .NotEmpty()
            .WithMessage("La persona es obligatoria para registrar la asistencia.");
    }
}