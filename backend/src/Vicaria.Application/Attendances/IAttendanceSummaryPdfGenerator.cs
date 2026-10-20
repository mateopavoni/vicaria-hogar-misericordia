namespace Vicaria.Application.Attendances;

public interface IAttendanceSummaryPdfGenerator
{
    byte[] GenerateMonthlySummaryPdf(MonthlyAttendanceSummaryDto summary);
}
