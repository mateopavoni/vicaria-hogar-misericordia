using FluentValidation;
using Vicaria.Domain.Entities;

namespace Vicaria.Application.CasonaVisits;

// la regla de "no en el pasado" no está acá porque en el PUT solo aplica si la fecha/hora
// cambiaron (si no, sería imposible marcar como Realizada una visita de hoy ya ocurrida);
// esa comparación contra lo guardado la resuelve el servicio.
public class UpdateCasonaVisitDtoValidator : AbstractValidator<UpdateCasonaVisitDto>
{
    public UpdateCasonaVisitDtoValidator()
    {
        RuleFor(x => x.PersonId)
            .NotEmpty().WithMessage("Debe indicar la persona que recibe la visita.");

        RuleFor(x => x.VisitorName)
            .NotEmpty().WithMessage("El nombre del visitante es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre del visitante no puede superar los 100 caracteres.");

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("La fecha de la visita es obligatoria.");

        RuleFor(x => x.EstimatedDurationMinutes)
            .GreaterThan(0).WithMessage("La duración estimada debe ser mayor a 0 minutos.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("El estado de la visita no es válido.");

        RuleFor(x => x.CancellationReason)
            .MaximumLength(500).WithMessage("El motivo de cancelación no puede superar los 500 caracteres.");

        When(x => x.Status == VisitStatus.Cancelled, () =>
        {
            RuleFor(x => x.CancellationReason)
                .NotEmpty().WithMessage("El motivo de cancelación es obligatorio cuando la visita se cancela.");
        });
    }
}
