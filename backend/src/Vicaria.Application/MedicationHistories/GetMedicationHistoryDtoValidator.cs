using FluentValidation;

namespace Vicaria.Application.MedicationHistories;

public class GetMedicationHistoryDtoValidator : AbstractValidator<GetMedicationHistoryDto>
{
    public GetMedicationHistoryDtoValidator()
    {
        RuleFor(x => x)
            .Must(x => !x.From.HasValue || !x.To.HasValue || x.From.Value <= x.To.Value)
            .WithMessage("La fecha 'desde' no puede ser posterior a la fecha 'hasta'.")
            .WithName("from");
    }
}
