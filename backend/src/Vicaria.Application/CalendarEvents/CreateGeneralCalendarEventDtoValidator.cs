using FluentValidation;

namespace Vicaria.Application.CalendarEvents;

public class CreateGeneralCalendarEventDtoValidator : AbstractValidator<CreateGeneralCalendarEventDto>
{
    public CreateGeneralCalendarEventDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("El título del evento es obligatorio.")
            .MaximumLength(100).WithMessage("El título no puede superar los 100 caracteres.");

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("La fecha del evento es obligatoria.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("La descripción no puede superar los 500 caracteres.");

        // StartTime y EndTime son opcionales e independientes; solo se comparan si vienen ambos
        When(x => x.StartTime.HasValue && x.EndTime.HasValue, () =>
        {
            RuleFor(x => x.EndTime!.Value)
                .GreaterThan(x => x.StartTime!.Value)
                .WithMessage("La hora de fin debe ser posterior a la hora de inicio.");
        });
    }
}
