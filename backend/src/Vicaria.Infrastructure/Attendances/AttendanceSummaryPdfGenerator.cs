using Vicaria.Application.Attendances;
using Vicaria.Infrastructure.Pdf;

namespace Vicaria.Infrastructure.Attendances;

// genera el PDF del resumen mensual de asistencia con PdfSharp;
// el contenido es el mismo MonthlyAttendanceSummaryDto que calcula
// AttendanceStatisticsService
public class AttendanceSummaryPdfGenerator : IAttendanceSummaryPdfGenerator
{
    public byte[] GenerateMonthlySummaryPdf(MonthlyAttendanceSummaryDto summary)
    {
        // antes cada línea se escribía con un helper que recibía la página por valor: al pasar de
        // página, el resto del listado se dibujaba encima del encabezado de la primera hoja
        using var writer = new PdfReportWriter($"Resumen mensual de asistencia — {summary.Month:00}/{summary.Year}");
        writer.Meta($"Generado el {PdfReportWriter.ToCordobaTime(DateTime.UtcNow):dd/MM/yyyy HH:mm}");

        if (summary.TotalRecords == 0)
        {
            writer.Section("Totales");
            writer.Paragraph("Sin registros de asistencia en este mes.", muted: true);
        }
        else
        {
            var totals = new List<(string Label, string Value)>
            {
                ("Total de registros", summary.TotalRecords.ToString()),
                ("Personas sin ficha (anónimas)", summary.TotalAnonymous.ToString()),
                ("Promedio de personas por día", summary.AveragePerDay.ToString("0.##")),
            };
            var byActivity = summary.TotalsByActivity.Select(t => (Label: t.ActivityName, Value: t.Count.ToString())).ToList();
            var comparison = new List<(string Label, string Value)>
            {
                ("Registros del mes anterior", summary.PreviousMonthTotal.ToString()),
                ("Variación", summary.ComparisonPercentage.HasValue
                    ? $"{summary.ComparisonPercentage.Value:+0.##;-0.##;0} %"
                    : "Sin datos del mes anterior"),
            };
            var labelWidth = writer.LabelWidthFor(totals.Concat(byActivity).Concat(comparison).Select(r => r.Label));

            writer.Section("Totales");
            foreach (var (label, value) in totals) writer.Field(label, value, labelWidth);
            writer.Section("Asistencia por actividad");
            foreach (var (label, value) in byActivity) writer.Field(label, value, labelWidth);
            writer.Section("Comparativa con el mes anterior");
            foreach (var (label, value) in comparison) writer.Field(label, value, labelWidth);
        }

        writer.Section($"Personas inactivas — más de 14 días sin asistir al cierre del mes ({summary.InactivePeople.Count})");
        if (summary.InactivePeople.Count == 0)
        {
            writer.Paragraph("Sin personas inactivas.", muted: true);
        }
        else
        {
            var rows = summary.InactivePeople.Select(person =>
            {
                var fullName = string.IsNullOrWhiteSpace(person.LastName)
                    ? person.FirstName
                    : $"{person.FirstName} {person.LastName}";
                var detail = person.LastAttendanceDate.HasValue
                    ? $"Última asistencia {person.LastAttendanceDate.Value:dd/MM/yyyy} ({person.DaysSinceLastAttendance} días)"
                    : "Sin asistencias registradas";
                return (Label: fullName, Value: detail);
            }).ToList();
            var nameWidth = writer.LabelWidthFor(rows.Select(r => r.Label));
            foreach (var (label, value) in rows) writer.Field(label, value, nameWidth);
        }

        return writer.Save();
    }
}
