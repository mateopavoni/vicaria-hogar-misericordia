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

        y = WriteLine(graphics, page, y, titleFont, $"Informe estadístico para Cáritas — {report.PeriodType}");
        y = WriteLine(graphics, page, y, bodyFont, $"Período: {report.From} al {report.To}");
        y = WriteLine(graphics, page, y, bodyFont, string.Empty);

        y = WriteLine(graphics, page, y, sectionFont, "Totales");
        y = WriteLine(graphics, page, y, bodyFont, $"Personas atendidas: {report.Totals.AttendedPersons}");
        y = WriteLine(graphics, page, y, bodyFont, $"Promedio de asistencias por persona: {report.Totals.AverageAttendancesPerPerson}");
        y = WriteLine(graphics, page, y, bodyFont, $"Nuevas altas en el período: {report.Totals.NewRegistrations}");
        y = WriteLine(graphics, page, y, bodyFont, string.Empty);

        y = WriteLine(graphics, page, y, sectionFont, "Género");
        y = WriteLine(graphics, page, y, bodyFont, $"Masculino: {report.Gender.Male}");
        y = WriteLine(graphics, page, y, bodyFont, $"Femenino: {report.Gender.Female}");
        y = WriteLine(graphics, page, y, bodyFont, $"Otro: {report.Gender.Other}");
        y = WriteLine(graphics, page, y, bodyFont, string.Empty);

        y = WriteLine(graphics, page, y, sectionFont, "Situación de vivienda");
        y = WriteLine(graphics, page, y, bodyFont, $"En situación de calle: {report.Housing.Street}");
        y = WriteLine(graphics, page, y, bodyFont, $"Con domicilio: {report.Housing.Housed}");
        y = WriteLine(graphics, page, y, bodyFont, string.Empty);

        y = WriteLine(graphics, page, y, sectionFont, "Documentación");
        y = WriteLine(graphics, page, y, bodyFont, $"Con DNI: {report.Dni.WithDni}");
        y = WriteLine(graphics, page, y, bodyFont, $"Sin DNI: {report.Dni.WithoutDni}");
        y = WriteLine(graphics, page, y, bodyFont, string.Empty);

        y = WriteLine(graphics, page, y, sectionFont, "Distribución geográfica");
        if (report.Geography.Count == 0)
        {
            y = WriteLine(graphics, page, y, bodyFont, "Sin datos.");
        }
        else
        {
            foreach (var item in report.Geography)
            {
                y = WriteLine(graphics, page, y, bodyFont, $"{item.Label}: {item.Count}");
            }
        }
        y = WriteLine(graphics, page, y, bodyFont, string.Empty);

        y = WriteLine(graphics, page, y, sectionFont, "Motivo de asistencia");
        y = WriteLine(graphics, page, y, bodyFont, $"Consumo de alcohol: {report.Reasons.Alcohol}");
        y = WriteLine(graphics, page, y, bodyFont, $"Consumo de drogas: {report.Reasons.Drugs}");
        y = WriteLine(graphics, page, y, bodyFont, $"Violencia: {report.Reasons.Violence}");
        y = WriteLine(graphics, page, y, bodyFont, $"Múltiple: {report.Reasons.Multiple}");
        y = WriteLine(graphics, page, y, bodyFont, $"No especificado: {report.Reasons.Unspecified}");

        using var stream = new MemoryStream();
        document.Save(stream);
        return stream.ToArray();
    }

    private static double WriteLine(XGraphics graphics, PdfPage page, double y, XFont font, string text)
    {
        if (y + LineHeight > page.Height.Point - Margin)
        {
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
