using FluentValidation;

namespace Vicaria.Application.MedicationStocks;

public class UpdateMedicationStockThresholdDtoValidator : AbstractValidator<UpdateMedicationStockThresholdDto>
{
    public UpdateMedicationStockThresholdDtoValidator()
    {
        RuleFor(x => x.MinimumStockThreshold)
            .GreaterThanOrEqualTo(0).WithMessage("El umbral mínimo de stock no puede ser negativo.");
    }
}