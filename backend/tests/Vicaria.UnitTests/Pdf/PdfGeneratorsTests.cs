using System.Text;
using Vicaria.Application.Attendances;
using Vicaria.Application.MedicationHistories;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Attendances;
using Vicaria.Infrastructure.MedicationHistories;
using Vicaria.Infrastructure.Pdf;

namespace Vicaria.UnitTests.Pdf;

// los PDF dependen de la licencia de PDFsharp y de que haya una fuente instalada: este test
// avisa si una actualización del paquete o una imagen sin fuentes los rompe
public class PdfGeneratorsTests
{
    private static void AssertIsPdf(byte[] bytes)
    {
        Assert.True(bytes.Length > 500);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public void GenerateMonthlySummaryPdf_ReturnsPdfDocument()
    {
        PdfFontResolver.Register();
        var summary = new MonthlyAttendanceSummaryDto(
            2026, 10, 12, 4, 0.5,
            new List<ActivityTotalDto> { new(AttendanceActivity.DayCenter, "Hogar de día", 12) },
            7, 71.4,
            new List<InactivePersonDto> { new(Guid.NewGuid(), "Ana", "Pérez", new DateTime(2026, 9, 1), 48) });

        AssertIsPdf(new AttendanceSummaryPdfGenerator().GenerateMonthlySummaryPdf(summary));
    }

    [Fact]
    public void GeneratePersonHistoryPdf_ReturnsPdfDocument()
    {
        PdfFontResolver.Register();
        var document = new MedicationHistoryPdfDocument(
            "Ana Pérez", "30111222", null, null, new List<MedicationHistoryEntryDto>(), DateTime.UtcNow);

        AssertIsPdf(new MedicationHistoryPdfGenerator().GeneratePersonHistoryPdf(document));
    }
}
