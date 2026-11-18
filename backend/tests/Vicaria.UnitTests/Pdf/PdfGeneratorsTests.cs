using System.Text;
using PdfSharp.Pdf.IO;
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

    private static int PageCount(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var pdf = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        return pdf.PageCount;
    }

    [Fact]
    public void GenerateMonthlySummaryPdf_ListaLargaDeInactivos_ContinuaEnPaginasNuevasSinPisarLaPrimera()
    {
        // con el helper viejo (página por valor) las líneas posteriores al salto se dibujaban
        // encima del encabezado de la página 1 y el PDF quedaba en 2 hojas
        var inactive = Enumerable.Range(1, 150)
            .Select(i => new InactivePersonDto(Guid.NewGuid(), $"Persona {i}", "Apellido", DateTime.UtcNow.AddDays(-20), 20))
            .ToList();
        var summary = new MonthlyAttendanceSummaryDto(2026, 11, 300, 4, 12.5,
            new List<ActivityTotalDto> { new(AttendanceActivity.DayCenter, "Hogar de día", 250) },
            200, 50, inactive);

        var bytes = new AttendanceSummaryPdfGenerator().GenerateMonthlySummaryPdf(summary);

        AssertIsPdf(bytes);
        Assert.True(PageCount(bytes) >= 4);
    }

    [Fact]
    public void GeneratePersonHistoryPdf_ObservacionesLargas_GeneraVariasPaginas()
    {
        var observation = string.Join(' ', Enumerable.Repeat("Se negó a tomarla y se informó a la Directora de la Casa.", 6));
        var entries = Enumerable.Range(0, 60)
            .Select(i => new MedicationHistoryEntryDto(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-i)), "08:00",
                Guid.NewGuid(), Guid.NewGuid(), "Enalapril 10 mg", "1 comprimido", i % 5 == 0 ? "not-administered" : "administered",
                i % 5 == 0 ? observation : null, null, null, null))
            .ToList();
        var document = new MedicationHistoryPdfDocument("Ana Pérez", "30111222", null, null, entries, DateTime.UtcNow);

        var bytes = new MedicationHistoryPdfGenerator().GeneratePersonHistoryPdf(document);

        AssertIsPdf(bytes);
        Assert.True(PageCount(bytes) >= 2);
    }
}
