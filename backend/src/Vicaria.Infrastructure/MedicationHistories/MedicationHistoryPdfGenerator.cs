using Vicaria.Application.MedicationHistories;
using Vicaria.Infrastructure.Pdf;

namespace Vicaria.Infrastructure.MedicationHistories;

// genera el PDF del historial de medicación de una persona con PdfSharp,
// con el mismo conjunto que el endpoint JSON más el encabezado para
// presentar a un profesional de salud (ver PdfReportWriter)
public class MedicationHistoryPdfGenerator : IMedicationHistoryPdfGenerator
{
    public byte[] GeneratePersonHistoryPdf(MedicationHistoryPdfDocument document)
    {
        using var writer = new PdfReportWriter("Historial de medicación");
        writer.Meta($"{document.PersonName}{FormatDni(document.PersonDni)}");
        writer.Meta($"Período: {FormatRange(document.From, document.To)} · Generado el {PdfReportWriter.ToCordobaTime(document.GeneratedAt):dd/MM/yyyy HH:mm}");

        if (document.Entries.Count == 0)
        {
            writer.Section("Detalle por toma");
            writer.Paragraph("Sin registros para los filtros indicados.", muted: true);
            return writer.Save();
        }

        var administered = document.Entries.Count(e => e.Status == "administered");
        var notAdministered = document.Entries.Count(e => e.Status == "not-administered");
        var unregistered = document.Entries.Count - administered - notAdministered;

        writer.Section("Resumen");
        var summary = new (string Label, string Value)[]
        {
            ("Total de tomas", document.Entries.Count.ToString()),
            ("Administradas", administered.ToString()),
            ("No administradas", notAdministered.ToString()),
            ("No registradas", unregistered.ToString()),
        };
        var summaryWidth = writer.LabelWidthFor(summary.Select(r => r.Label));
        foreach (var (label, value) in summary)
        {
            writer.Field(label, value, summaryWidth);
        }
        if (notAdministered > 0)
        {
            writer.Alert($"Atención: {notAdministered} toma(s) no administrada(s) en el período. Ver el detalle y sus observaciones.");
        }

        writer.Section("Detalle por toma");
        var rows = document.Entries.Select(entry =>
        {
            var status = entry.Status switch
            {
                "administered" => "Administrada",
                "not-administered" => "NO ADMINISTRADA",
                _ => "No registrada"
            };
            var value = $"{entry.MedicationName} {entry.Dose} — {status}";
            if (!string.IsNullOrWhiteSpace(entry.Observation))
            {
                value += $". Obs.: {entry.Observation}";
            }
            return (Label: $"{entry.Date:dd/MM/yyyy} {entry.Time}", Value: value);
        }).ToList();
        var detailWidth = writer.LabelWidthFor(rows.Select(r => r.Label));
        foreach (var (label, value) in rows)
        {
            writer.Field(label, value, detailWidth);
        }

        return writer.Save();
    }

    private static string FormatDni(string? dni) =>
        string.IsNullOrWhiteSpace(dni) ? string.Empty : $" — DNI {dni}";

    private static string FormatRange(DateOnly? from, DateOnly? to) =>
        (from, to) switch
        {
            (not null, not null) => $"{from.Value:dd/MM/yyyy} al {to.Value:dd/MM/yyyy}",
            (not null, null) => $"desde {from.Value:dd/MM/yyyy}",
            (null, not null) => $"hasta {to.Value:dd/MM/yyyy}",
            _ => "sin límites"
        };
}
