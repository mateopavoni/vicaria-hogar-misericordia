using PdfSharp.Drawing;
using PdfSharp.Pdf;
using Vicaria.Application.PersonExports;

namespace Vicaria.Infrastructure.PersonExports;

// genera el PDF de exportación de la ficha (SCRUM-252) con PdfSharp;
// sigue el estilo de MedicationHistoryPdfGenerator (PdfWriter interno)
public class PersonExportPdfGenerator : IPersonExportPdfGenerator
{
    private const double Margin = 50;
    private const double LineHeight = 18;
    private const string SystemName = "Vicaria - Sistema de Gestión Nuestra Señora de la Misericordia";

    public byte[] GenerateExportPdf(PersonExportDocument document)
    {
        var personName = string.IsNullOrWhiteSpace(document.LastName)
            ? document.FirstName
            : $"{document.FirstName} {document.LastName}";

        using var writer = new PdfWriter($"Ficha de {personName}");

        var titleFont = new XFont("Helvetica", 16, XFontStyleEx.Bold);
        var sectionFont = new XFont("Helvetica", 12, XFontStyleEx.Bold);
        var bodyFont = new XFont("Helvetica", 11, XFontStyleEx.Regular);

        writer.WriteLine(titleFont, $"Ficha de {personName}");
        writer.WriteLine(bodyFont, SystemName);
        writer.WriteLine(bodyFont, $"Fecha de exportación: {ToArgentinaTime(document.GeneratedAtUtc):dd/MM/yyyy HH:mm} (Argentina)");
        writer.WriteLine(bodyFont, $"Generado por: {document.GeneratedByName}{FormatRole(document.GeneratedByRole)}");
        writer.WriteLine(bodyFont, string.Empty);

        writer.WriteLine(sectionFont, "Datos personales");
        writer.WriteLine(bodyFont, $"Nombre: {personName}{FormatDni(document.Dni)}");
        if (document.DateOfBirth.HasValue)
        {
            writer.WriteLine(bodyFont, $"Fecha de nacimiento: {document.DateOfBirth.Value:dd/MM/yyyy}");
        }
        if (!string.IsNullOrWhiteSpace(document.Phone))
        {
            writer.WriteLine(bodyFont, $"Teléfono: {document.Phone}");
        }
        if (!string.IsNullOrWhiteSpace(document.PersonType))
        {
            writer.WriteLine(bodyFont, $"Tipo: {document.PersonType}");
        }
        if (!string.IsNullOrWhiteSpace(document.ReasonForEntry))
        {
            writer.WriteLine(bodyFont, $"Motivo de ingreso: {document.ReasonForEntry}");
        }
        if (document.EntryDate.HasValue)
        {
            writer.WriteLine(bodyFont, $"Fecha de ingreso: {document.EntryDate.Value:dd/MM/yyyy}");
        }
        if (!string.IsNullOrWhiteSpace(document.HousingSituation))
        {
            writer.WriteLine(bodyFont, $"Situación habitacional: {document.HousingSituation}");
        }
        if (!string.IsNullOrWhiteSpace(document.OvernightLocation))
        {
            writer.WriteLine(bodyFont, $"Lugar de pernocte: {document.OvernightLocation}");
        }
        if (!string.IsNullOrWhiteSpace(document.Occupation))
        {
            writer.WriteLine(bodyFont, $"Ocupación: {document.Occupation}");
        }
        writer.WriteLine(bodyFont, $"Documentación: {(document.HasDocumentation ? "Sí" : "No")}");
        if (!string.IsNullOrWhiteSpace(document.GeneralNotes))
        {
            writer.WriteLine(bodyFont, $"Notas generales: {document.GeneralNotes}");
        }
        writer.WriteLine(bodyFont, string.Empty);

        if (document.Sections is PersonExportSections.DataWithObservations or PersonExportSections.All)
        {
            writer.WriteLine(sectionFont, "Observaciones");
            if (document.Observations.Count == 0)
            {
                writer.WriteLine(bodyFont, "Sin observaciones registradas.");
            }
            foreach (var observation in document.Observations)
            {
                var category = string.IsNullOrWhiteSpace(observation.CategoryName)
                    ? string.Empty
                    : $" [{observation.CategoryName}]";
                writer.WriteLine(bodyFont, $"{ToArgentinaTime(observation.CreatedAt):dd/MM/yyyy HH:mm}{category}");
                writer.WriteLine(bodyFont, observation.Content);
                writer.WriteLine(bodyFont, string.Empty);
            }
        }

        if (document.Sections == PersonExportSections.All)
        {
            writer.WriteLine(sectionFont, "Historia de vida");
            writer.WriteLine(bodyFont, $"Antes del Centro Barrial: {Fallback(document.BeforeCentroBarrial)}");
            writer.WriteLine(bodyFont, $"En el Centro Barrial: {Fallback(document.InCentroBarrial)}");
            writer.WriteLine(bodyFont, $"Después del Centro Barrial: {Fallback(document.AfterCentroBarrial)}");
            writer.WriteLine(bodyFont, string.Empty);
        }

        return writer.Save();
    }

    private static string FormatDni(string? dni) =>
        string.IsNullOrWhiteSpace(dni) ? string.Empty : $" — DNI {dni}";

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

    private sealed class PdfWriter : IDisposable
    {
        private readonly PdfDocument _document;
        private PdfPage _page;
        private XGraphics _graphics;
        private double _y;

        public PdfWriter(string title)
        {
            _document = new PdfDocument();
            _document.Info.Title = title;
            _page = _document.AddPage();
            _graphics = XGraphics.FromPdfPage(_page);
            _y = Margin;
        }

        // parte el texto en líneas que entren en el ancho de la página (observaciones e
        // historia de vida suelen ser párrafos largos)
        public void WriteLine(XFont font, string text)
        {
            var maxWidth = _page.Width.Point - Margin * 2;
            foreach (var paragraph in text.Replace("\r", string.Empty).Split('\n'))
            {
                var line = string.Empty;
                foreach (var word in paragraph.Split(' '))
                {
                    var candidate = line.Length == 0 ? word : $"{line} {word}";
                    if (line.Length > 0 && _graphics.MeasureString(candidate, font).Width > maxWidth)
                    {
                        DrawLine(font, line);
                        line = word;
                    }
                    else
                    {
                        line = candidate;
                    }
                }
                DrawLine(font, line);
            }
        }

        private void DrawLine(XFont font, string text)
        {
            if (_y + LineHeight > _page.Height.Point - Margin)
            {
                _page = _document.AddPage();
                _graphics = XGraphics.FromPdfPage(_page);
                _y = Margin;
            }

            _graphics.DrawString(text, font, XBrushes.Black, new XPoint(Margin, _y), XStringFormats.TopLeft);
            _y += LineHeight;
        }

        public byte[] Save()
        {
            using var stream = new MemoryStream();
            _document.Save(stream);
            return stream.ToArray();
        }

        public void Dispose() => _document.Dispose();
    }
}
