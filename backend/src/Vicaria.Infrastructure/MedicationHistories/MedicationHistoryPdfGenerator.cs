using PdfSharp.Drawing;
using PdfSharp.Pdf;
using Vicaria.Application.MedicationHistories;

namespace Vicaria.Infrastructure.MedicationHistories;

// genera el PDF del historial de medicación de una persona (SCRUM-241) con PdfSharp,
// con el mismo conjunto que el endpoint JSON (SCRUM-240) más el encabezado para
// presentar a un profesional de salud; sigue el estilo de AttendanceSummaryPdfGenerator
public class MedicationHistoryPdfGenerator : IMedicationHistoryPdfGenerator
{
    private const double Margin = 50;
    private const double LineHeight = 18;

    public byte[] GeneratePersonHistoryPdf(MedicationHistoryPdfDocument document)
    {
        using var writer = new PdfWriter($"Historial de medicación — {document.PersonName}");

        var titleFont = new XFont("Helvetica", 16, XFontStyleEx.Bold);
        var sectionFont = new XFont("Helvetica", 12, XFontStyleEx.Bold);
        var bodyFont = new XFont("Helvetica", 11, XFontStyleEx.Regular);

        writer.WriteLine(titleFont, "Historial de medicación");
        writer.WriteLine(bodyFont, $"Persona: {document.PersonName}{FormatDni(document.PersonDni)}");
        writer.WriteLine(bodyFont, $"Rango: {FormatRange(document.From, document.To)}");
        writer.WriteLine(bodyFont, $"Generado: {document.GeneratedAt:dd/MM/yyyy HH:mm} UTC");
        writer.WriteLine(bodyFont, string.Empty);

        if (document.Entries.Count == 0)
        {
            writer.WriteLine(bodyFont, "Sin registros para los filtros indicados.");
            return writer.Save();
        }

        var administered = document.Entries.Count(e => e.Status == "administered");
        var notAdministered = document.Entries.Count(e => e.Status == "not-administered");
        var unregistered = document.Entries.Count - administered - notAdministered;

        writer.WriteLine(sectionFont, "Resumen");
        writer.WriteLine(bodyFont, $"Total de tomas: {document.Entries.Count}");
        writer.WriteLine(bodyFont, $"Administradas: {administered}");
        writer.WriteLine(bodyFont, $"*** NO ADMINISTRADAS: {notAdministered} ***");
        writer.WriteLine(bodyFont, $"No registradas: {unregistered}");
        writer.WriteLine(bodyFont, string.Empty);

        writer.WriteLine(sectionFont, "Detalle por toma");
        foreach (var entry in document.Entries)
        {
            var statusText = entry.Status switch
            {
                "administered" => "Administrada",
                "not-administered" => "*** NO ADMINISTRADA ***",
                _ => "No registrada"
            };
            writer.WriteLine(bodyFont, $"{entry.Date:dd/MM/yyyy} {entry.Time} — {entry.MedicationName} {entry.Dose} — {statusText}");
            if (!string.IsNullOrWhiteSpace(entry.Observation))
            {
                writer.WriteLine(bodyFont, $"    Obs.: {entry.Observation}");
            }
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

        public void WriteLine(XFont font, string text)
        {
            if (_y + LineHeight > _page.Height.Point - Margin)
            {
                _page = _document.AddPage();
                _graphics = XGraphics.FromPdfPage(_page);
                _y = Margin;
            }

            _graphics.DrawString(text, font, XBrushes.Black,
                new XRect(XUnit.FromPoint(Margin), XUnit.FromPoint(_y), XUnit.FromPoint(_page.Width.Point - Margin * 2), XUnit.FromPoint(LineHeight)),
                XStringFormats.TopLeft);
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
