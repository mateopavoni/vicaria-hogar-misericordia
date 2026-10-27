using PdfSharp.Drawing;
using PdfSharp.Pdf;
using Vicaria.Application.CaritasReports;

namespace Vicaria.Infrastructure.CaritasReports;

// versión imprimible del informe Caritas (SCRUM-270) con PdfSharp;
// mismo contenido que el JSON de CaritasReportService, en el estilo de
// AttendanceSummaryPdfGenerator
public class CaritasReportPdfGenerator : ICaritasReportPdfGenerator
{
    private const double Margin = 50;
    private const double LineHeight = 18;

    public byte[] GenerateReportPdf(CaritasReportDto report)
    {
        using var document = new PdfDocument();
        document.Info.Title = $"Informe Caritas {report.PeriodType} {report.From} al {report.To}";

        var titleFont = new XFont("Helvetica", 16, XFontStyleEx.Bold);
        var sectionFont = new XFont("Helvetica", 12, XFontStyleEx.Bold);
        var bodyFont = new XFont("Helvetica", 11, XFontStyleEx.Regular);

        var page = document.AddPage();
        var graphics = XGraphics.FromPdfPage(page);
        var y = Margin;

        y = WriteLine(ref graphics, ref page, y, titleFont, $"Informe estadístico para Cáritas — {report.PeriodType}");
        y = WriteLine(ref graphics, ref page, y, bodyFont, $"Período: {report.From} al {report.To}");
        y = WriteLine(ref graphics, ref page, y, bodyFont, string.Empty);

        y = WriteLine(ref graphics, ref page, y, sectionFont, "Totales");
        y = WriteLine(ref graphics, ref page, y, bodyFont, $"Personas atendidas: {report.Totals.AttendedPersons}");
        y = WriteLine(ref graphics, ref page, y, bodyFont, $"Promedio de asistencias por persona: {report.Totals.AverageAttendancesPerPerson}");
        y = WriteLine(ref graphics, ref page, y, bodyFont, $"Nuevas altas en el período: {report.Totals.NewRegistrations}");
        y = WriteLine(ref graphics, ref page, y, bodyFont, string.Empty);

        var categories = new (string Title, IReadOnlyList<CaritasReportCountItemDto> Items)[]
        {
            ("Género", report.Gender),
            ("Situación de vivienda", report.Housing),
            ("Documentación", report.Dni),
            ("Distribución geográfica", report.Geography),
            ("Motivo de asistencia", report.Reasons)
        };

        foreach (var (title, items) in categories)
        {
            y = WriteLine(ref graphics, ref page, y, sectionFont, title);
            if (items.Count == 0)
            {
                y = WriteLine(ref graphics, ref page, y, bodyFont, "Sin datos.");
            }
            foreach (var item in items)
            {
                y = WriteLine(ref graphics, ref page, y, bodyFont, $"{item.Label}: {item.Count}");
            }
            y = WriteLine(ref graphics, ref page, y, bodyFont, string.Empty);
        }

        using var stream = new MemoryStream();
        document.Save(stream);
        return stream.ToArray();
    }

    // page y graphics por ref: al saltar de página, las líneas siguientes se escriben en la nueva
    private static double WriteLine(ref XGraphics graphics, ref PdfPage page, double y, XFont font, string text)
    {
        if (y + LineHeight > page.Height.Point - Margin)
        {
            graphics.Dispose();
            page = page.Owner.AddPage();
            graphics = XGraphics.FromPdfPage(page);
            y = Margin;
        }

        graphics.DrawString(text, font, XBrushes.Black,
            new XRect(XUnit.FromPoint(Margin), XUnit.FromPoint(y), XUnit.FromPoint(page.Width.Point - Margin * 2), XUnit.FromPoint(LineHeight)),
            XStringFormats.TopLeft);
        return y + LineHeight;
    }
}
