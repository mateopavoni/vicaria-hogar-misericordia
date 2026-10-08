using FluentValidation;

namespace Vicaria.Application.CalendarEvents;

public class UpdateGeneralCalendarEventDtoValidator : AbstractValidator<UpdateGeneralCalendarEventDto>
{
    public UpdateGeneralCalendarEventDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("El título es obligatorio.")
            .MaximumLength(150).WithMessage("El título no puede superar los 150 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("La descripción no puede superar los 500 caracteres.");

        RuleFor(x => x)
            .Must(x => !x.StartTime.HasValue || !x.EndTime.HasValue || x.StartTime <= x.EndTime)
            .WithMessage("La hora de inicio no puede ser posterior a la hora de fin.");
    }
}