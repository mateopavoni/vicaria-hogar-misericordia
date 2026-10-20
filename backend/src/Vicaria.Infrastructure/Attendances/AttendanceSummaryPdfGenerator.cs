using PdfSharp.Drawing;
using PdfSharp.Pdf;
using Vicaria.Application.Attendances;

namespace Vicaria.Infrastructure.Attendances;

// genera el PDF del resumen mensual de asistencia (SCRUM-225) con PdfSharp;
// el contenido es el mismo MonthlyAttendanceSummaryDto que calcula
// AttendanceStatisticsService (y que SCRUM-224 reutiliza)
public class AttendanceSummaryPdfGenerator : IAttendanceSummaryPdfGenerator
{
    private const double Margin = 50;
    private const double LineHeight = 18;

    public byte[] GenerateMonthlySummaryPdf(MonthlyAttendanceSummaryDto summary)
    {
        using var document = new PdfDocument();
        document.Info.Title = $"Resumen mensual de asistencia {summary.Month:00}/{summary.Year}";

        var titleFont = new XFont("Helvetica", 16, XFontStyleEx.Bold);
        var sectionFont = new XFont("Helvetica", 12, XFontStyleEx.Bold);
        var bodyFont = new XFont("Helvetica", 11, XFontStyleEx.Regular);

        var page = document.AddPage();
        var graphics = XGraphics.FromPdfPage(page);
        var y = Margin;

        y = WriteLine(graphics, page, y, titleFont, $"Resumen mensual de asistencia — {summary.Month:00}/{summary.Year}");
        y = WriteLine(graphics, page, y, bodyFont, string.Empty);

        if (summary.TotalRecords == 0)
        {
            y = WriteLine(graphics, page, y, bodyFont, "Sin registros de asistencia en este mes.");
        }
        else
        {
            y = WriteLine(graphics, page, y, sectionFont, "Totales");
            y = WriteLine(graphics, page, y, bodyFont, $"Total de registros: {summary.TotalRecords}");
            y = WriteLine(graphics, page, y, bodyFont, $"Personas sin ficha (anónimas): {summary.TotalAnonymous}");
            y = WriteLine(graphics, page, y, bodyFont, $"Promedio de personas por día: {summary.AveragePerDay}");
            y = WriteLine(graphics, page, y, bodyFont, string.Empty);

            y = WriteLine(graphics, page, y, sectionFont, "Asistencia por actividad");
            foreach (var total in summary.TotalsByActivity)
            {
                y = WriteLine(graphics, page, y, bodyFont, $"{total.ActivityName}: {total.Count}");
            }
            y = WriteLine(graphics, page, y, bodyFont, string.Empty);

            y = WriteLine(graphics, page, y, sectionFont, "Comparativa con el mes anterior");
            y = WriteLine(graphics, page, y, bodyFont, $"Registros del mes anterior: {summary.PreviousMonthTotal}");
            y = WriteLine(graphics, page, y, bodyFont, summary.ComparisonPercentage.HasValue
                ? $"Variación: {summary.ComparisonPercentage.Value:+0.##;-0.##;0} %"
                : "Variación: sin datos del mes anterior.");
            y = WriteLine(graphics, page, y, bodyFont, string.Empty);
        }

        y = WriteLine(graphics, page, y, sectionFont,
            $"Personas inactivas — más de 14 días sin asistir al cierre del mes ({summary.InactivePeople.Count})");
        if (summary.InactivePeople.Count == 0)
        {
            WriteLine(graphics, page, y, bodyFont, "Sin personas inactivas.");
        }
        else
        {
            foreach (var person in summary.InactivePeople)
            {
                var fullName = string.IsNullOrWhiteSpace(person.LastName)
                    ? person.FirstName
                    : $"{person.FirstName} {person.LastName}";
                var detail = person.LastAttendanceDate.HasValue
                    ? $"última asistencia {person.LastAttendanceDate.Value:dd/MM/yyyy} ({person.DaysSinceLastAttendance} días)"
                    : "sin asistencias registradas";
                y = WriteLine(graphics, page, y, bodyFont, $"{fullName} — {detail}");
            }
        }

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
