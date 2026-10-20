namespace Vicaria.Application.Attendances;

// promedios de asistencia por rango de fechas (SCRUM-224): misma fórmula que el
// resumen mensual (registros + anónimos / días calendario, días sin registros = 0)
public record WeeklyAverageDto(
    DateTime WeekStart,
    double AveragePerDay,
    int TotalRecords,
    int TotalAnonymous
);

public record MonthlyAverageDto(
    int Year,
    int Month,
    double AveragePerDay,
    int TotalRecords,
    int TotalAnonymous
);

public record AttendanceStatisticsDto(
    DateTime From,
    DateTime To,
    string? Activity,
    int TotalRecords,
    int TotalAnonymous,
    double AveragePerDay,
    IReadOnlyList<ActivityTotalDto> TotalsByActivity,
    int PreviousPeriodTotal,
    double? ComparisonPercentage,
    IReadOnlyList<WeeklyAverageDto> WeeklyAverages,
    IReadOnlyList<MonthlyAverageDto> MonthlyAverages
);

public record GetStatisticsQueryDto(DateTime From, DateTime To, string? Activity);

public class GetStatisticsResult
{
    public bool Success { get; init; }
    public AttendanceStatisticsDto? Data { get; init; }
    public string? ErrorMessage { get; init; }

    public static GetStatisticsResult Ok(AttendanceStatisticsDto data) =>
        new() { Success = true, Data = data };
}
