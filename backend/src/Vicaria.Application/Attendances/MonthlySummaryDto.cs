using Vicaria.Domain.Entities;

namespace Vicaria.Application.Attendances;

// resumen mensual de asistencia; el PDF y las estadísticas usan el mismo cálculo
public record ActivityTotalDto(
    AttendanceActivity Activity,
    string ActivityName,
    int Count
);

public record MonthlyAttendanceSummaryDto(
    int Year,
    int Month,
    int TotalRecords,
    int TotalAnonymous,
    double AveragePerDay,
    IReadOnlyList<ActivityTotalDto> TotalsByActivity,
    int PreviousMonthTotal,
    double? ComparisonPercentage,
    IReadOnlyList<InactivePersonDto> InactivePeople
);

public record GetMonthlySummaryQueryDto(int Year, int Month);

public class GetMonthlySummaryResult
{
    public bool Success { get; init; }
    public MonthlyAttendanceSummaryDto? Data { get; init; }
    public string? ErrorMessage { get; init; }

    public static GetMonthlySummaryResult Ok(MonthlyAttendanceSummaryDto data) =>
        new() { Success = true, Data = data };
}
