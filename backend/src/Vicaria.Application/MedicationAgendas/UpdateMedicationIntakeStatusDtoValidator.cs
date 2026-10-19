using FluentValidation;
using Vicaria.Domain.Entities;

namespace Vicaria.Application.MedicationAgendas;

public class UpdateMedicationIntakeStatusDtoValidator : AbstractValidator<UpdateMedicationIntakeStatusDto>
{
    public UpdateMedicationIntakeStatusDtoValidator()
    {
        RuleFor(x => x.Status)
            .Must(s => s == MedicationIntakeStatus.Administered || s == MedicationIntakeStatus.NotAdministered)
            .WithMessage("El estado debe ser Administrada o No administrada.");

        RuleFor(x => x.Observation)
            .MaximumLength(500).WithMessage("La observación no puede superar los 500 caracteres.");
    }
}