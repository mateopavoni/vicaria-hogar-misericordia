using FluentValidation;

namespace Vicaria.Application.MedicationStocks;

public class RegisterMedicationStockEntryDtoValidator : AbstractValidator<RegisterMedicationStockEntryDto>
{
    public RegisterMedicationStockEntryDtoValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del medicamento es obligatorio.")
            .MaximumLength(200).WithMessage("El nombre no puede superar los 200 caracteres.");

        RuleFor(x => x.Cantidad)
            .GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0.");

        RuleFor(x => x.FechaVencimiento)
            .NotEmpty().WithMessage("La fecha de vencimiento es obligatoria.")
            .Must(date => date.Date > DateTime.UtcNow.Date)
            .WithMessage("La fecha de vencimiento debe ser posterior a la fecha actual.");

        RuleFor(x => x.Proveedor)
            .MaximumLength(200).WithMessage("El proveedor no puede superar los 200 caracteres.");
    }
}