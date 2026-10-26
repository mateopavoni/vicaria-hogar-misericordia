namespace Vicaria.Application.CaritasReports;

public interface ICaritasReportService
{
    Task<CaritasReportResult> GetReportAsync(string type, DateTime to, CancellationToken cancellationToken = default);
}
