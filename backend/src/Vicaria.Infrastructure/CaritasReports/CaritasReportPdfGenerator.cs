using Vicaria.Application.CaritasReports;

using Vicaria.Infrastructure.Pdf;

namespace Vicaria.Infrastructure.CaritasReports;

// versión imprimible del informe Caritas con PdfSharp;
// mismo contenido que el JSON de CaritasReportService, en el estilo de
// AttendanceSummaryPdfGenerator
public class CaritasReportPdfGenerator : ICaritasReportPdfGenerator
{

    public byte[] GenerateReportPdf(CaritasReportDto report)
    {
        var period = report.PeriodType == "semestral" ? "Semestral" : "Trimestral";
        using var writer = new PdfReportWriter($"Informe estadístico para Cáritas — {period}");
        writer.Meta($"Período: {FormatDate(report.From)} al {FormatDate(report.To)} · Generado el {PdfReportWriter.ToCordobaTime(DateTime.UtcNow):dd/MM/yyyy HH:mm}");
        if (report.IsSample)
        {
            writer.Alert("DATOS DE EJEMPLO: no usar para la rendición a Cáritas.");
        }

        var totals = new (string Label, string Value)[]
        {
            ("Personas atendidas", report.Totals.AttendedPersons.ToString()),
            ("Promedio de asistencias por persona", report.Totals.AverageAttendancesPerPerson.ToString("0.0")),
            ("Nuevas altas en el período", report.Totals.NewRegistrations.ToString()),
        };
        var categories = new (string Title, IReadOnlyList<CaritasReportCountItemDto> Items)[]
        {
            ("Género", report.Gender),
            ("Situación de vivienda", report.Housing),
            ("Documentación", report.Dni),
            ("Distribución geográfica", report.Geography),
            ("Motivo de asistencia", report.Reasons)
        };

        var labelWidth = writer.LabelWidthFor(totals.Select(t => t.Label)
            .Concat(categories.SelectMany(c => c.Items.Select(i => i.Label))));

        writer.Section("Totales del período");
        foreach (var (label, value) in totals)
        {
            writer.Field(label, value, labelWidth);
        }

        foreach (var (title, items) in categories)
        {
            writer.Section(title);
            if (items.Count == 0)
            {
                writer.Paragraph("Sin datos.", muted: true);
                continue;
            }
            var total = items.Sum(i => i.Count);
            foreach (var item in items)
            {
                var percent = total > 0 ? item.Count * 100.0 / total : 0;
                writer.Field(item.Label, $"{item.Count} ({percent:0.0} %)", labelWidth);
            }
        }

        return writer.Save();
    }

    private static string FormatDate(string isoDate) =>
        DateTime.TryParse(isoDate, out var date) ? date.ToString("dd/MM/yyyy") : isoDate;
}
