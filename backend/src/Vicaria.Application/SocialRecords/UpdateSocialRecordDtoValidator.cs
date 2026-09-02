using FluentValidation;

namespace Vicaria.Application.SocialRecords;

public class UpdateSocialRecordDtoValidator : AbstractValidator<UpdateSocialRecordDto>
{
    public UpdateSocialRecordDtoValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede superar los 100 caracteres.");

        RuleFor(x => x.LastName).MaximumLength(100).WithMessage("El apellido no puede superar los 100 caracteres.");
        RuleFor(x => x.Dni).MaximumLength(20).WithMessage("El DNI no puede superar los 20 caracteres.");
        RuleFor(x => x.Phone).MaximumLength(30).WithMessage("El teléfono no puede superar los 30 caracteres.");
        RuleFor(x => x.ReasonForEntry).MaximumLength(500).WithMessage("El motivo de ingreso no puede superar los 500 caracteres.");
        RuleFor(x => x.HousingSituation).MaximumLength(200).WithMessage("La situación habitacional no puede superar los 200 caracteres.");
        RuleFor(x => x.OvernightLocation).MaximumLength(200).WithMessage("El lugar de pernoctación no puede superar los 200 caracteres.");
        RuleFor(x => x.Occupation).MaximumLength(200).WithMessage("La ocupación no puede superar los 200 caracteres.");
        RuleFor(x => x.GeneralNotes).MaximumLength(2000).WithMessage("Las observaciones generales no pueden superar los 2000 caracteres.");
    }
}
