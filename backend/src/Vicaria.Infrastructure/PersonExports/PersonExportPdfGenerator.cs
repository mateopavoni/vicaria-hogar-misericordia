using Vicaria.Application.PersonExports;
using Vicaria.Infrastructure.Pdf;

namespace Vicaria.Infrastructure.PersonExports;

// genera el PDF de exportación de la ficha con PdfSharp (ver PdfReportWriter)
public class PersonExportPdfGenerator : IPersonExportPdfGenerator
{
    public byte[] GenerateExportPdf(PersonExportDocument document)
    {
        var personName = string.IsNullOrWhiteSpace(document.LastName)
            ? document.FirstName
            : $"{document.FirstName} {document.LastName}";

        using var writer = new PdfReportWriter($"Ficha de {personName}");
        writer.Meta($"Exportada el {ToArgentinaTime(document.GeneratedAtUtc):dd/MM/yyyy HH:mm} (Argentina) por {document.GeneratedByName}{FormatRole(document.GeneratedByRole)}");

        var fields = new List<(string Label, string? Value)>
        {
            ("Nombre", personName),
            ("DNI", string.IsNullOrWhiteSpace(document.Dni) ? "Sin DNI" : document.Dni),
        };
        if (document.DateOfBirth.HasValue) fields.Add(("Fecha de nacimiento", $"{document.DateOfBirth.Value:dd/MM/yyyy}"));
        if (!string.IsNullOrWhiteSpace(document.Phone)) fields.Add(("Teléfono", document.Phone));
        if (!string.IsNullOrWhiteSpace(document.PersonType)) fields.Add(("Tipo", document.PersonType));
        if (!string.IsNullOrWhiteSpace(document.ReasonForEntry)) fields.Add(("Motivo de ingreso", document.ReasonForEntry));
        if (document.EntryDate.HasValue) fields.Add(("Fecha de ingreso", $"{document.EntryDate.Value:dd/MM/yyyy}"));
        if (!string.IsNullOrWhiteSpace(document.HousingSituation)) fields.Add(("Situación habitacional", document.HousingSituation));
        if (!string.IsNullOrWhiteSpace(document.OvernightLocation)) fields.Add(("Lugar de pernocte", document.OvernightLocation));
        if (!string.IsNullOrWhiteSpace(document.Occupation)) fields.Add(("Ocupación", document.Occupation));
        fields.Add(("Documentación", document.HasDocumentation ? "Sí" : "No"));
        if (!string.IsNullOrWhiteSpace(document.GeneralNotes)) fields.Add(("Notas generales", document.GeneralNotes));

        writer.Section("Datos personales");
        var labelWidth = writer.LabelWidthFor(fields.Select(f => f.Label));
        foreach (var (label, value) in fields)
        {
            writer.Field(label, value, labelWidth);
        }

        if (document.Sections is PersonExportSections.DataWithObservations or PersonExportSections.All)
        {
            writer.Section("Observaciones");
            if (document.Observations.Count == 0)
            {
                writer.Paragraph("Sin observaciones registradas.", muted: true);
            }
            foreach (var observation in document.Observations)
            {
                var category = string.IsNullOrWhiteSpace(observation.CategoryName)
                    ? string.Empty
                    : $" — {observation.CategoryName}";
                writer.Heading($"{ToArgentinaTime(observation.CreatedAt):dd/MM/yyyy HH:mm}{category}");
                writer.Paragraph(observation.Content);
            }
        }

        if (document.Sections == PersonExportSections.All)
        {
            writer.Section("Historia de vida");
            writer.Heading("Antes del Centro Barrial");
            writer.Paragraph(Fallback(document.BeforeCentroBarrial));
            writer.Heading("En el Centro Barrial");
            writer.Paragraph(Fallback(document.InCentroBarrial));
            writer.Heading("Después del Centro Barrial");
            writer.Paragraph(Fallback(document.AfterCentroBarrial));
        }

        return writer.Save();
    }

    private static string FormatRole(string role) =>
        string.IsNullOrWhiteSpace(role) ? string.Empty : $" ({role})";

    private static string Fallback(string? content) =>
        string.IsNullOrWhiteSpace(content) ? "Sin registro." : content;

    private static DateTime ToArgentinaTime(DateTime utc)
    {
        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Cordoba");
            return TimeZoneInfo.ConvertTimeFromUtc(utc, timeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            return utc.AddHours(-3);
        }
        catch (InvalidTimeZoneException)
        {
            return utc.AddHours(-3);
        }
    }
}
