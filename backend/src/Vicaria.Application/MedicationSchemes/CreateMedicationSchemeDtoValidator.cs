using FluentValidation;

namespace Vicaria.Application.MedicationSchemes;

public class CreateMedicationSchemeDtoValidator : AbstractValidator<CreateMedicationSchemeDto>
{
    public CreateMedicationSchemeDtoValidator()
    {
        RuleFor(x => x.MedicationId)
            .NotEmpty().WithMessage("Debe indicar el medicamento del catálogo.");

        RuleFor(x => x.Dose)
            .NotEmpty().WithMessage("La dosis es obligatoria.")
            .MaximumLength(200).WithMessage("La dosis no puede superar los 200 caracteres.");

        RuleFor(x => x.Time)
            .NotEmpty().WithMessage("La hora es obligatoria.")
            .Matches(@"^([01]\d|2[0-3]):[0-5]\d$").WithMessage("La hora no es válida (formato esperado HH:mm).");
    }
}
