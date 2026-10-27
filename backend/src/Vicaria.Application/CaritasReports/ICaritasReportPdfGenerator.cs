namespace Vicaria.Application.CaritasReports;

public interface ICaritasReportPdfGenerator
{
    byte[] GenerateReportPdf(CaritasReportDto report);
}
