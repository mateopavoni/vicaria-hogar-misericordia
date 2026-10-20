using FluentValidation;

namespace Vicaria.Application.Attendances;

public class GetPersonAttendanceHistoryValidator : AbstractValidator<GetPersonAttendanceHistoryFilterDto>
{
    public GetPersonAttendanceHistoryValidator()
    {
        RuleFor(x => x)
            .Must(x => !x.FromDate.HasValue || !x.ToDate.HasValue || x.FromDate.Value <= x.ToDate.Value)
            .WithMessage("La fecha desde no puede ser posterior a la fecha hasta.");
    }
}