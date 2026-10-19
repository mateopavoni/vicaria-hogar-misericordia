namespace Vicaria.Application.Attendances;

public interface IAttendanceStatisticsService
{
    Task<GetInactivePeopleResult> GetInactivePeopleAsync(int days, DateTime? asOfDate = null, CancellationToken cancellationToken = default);
    Task<GetMonthlySummaryResult> GetMonthlySummaryAsync(int year, int month, CancellationToken cancellationToken = default);
}
