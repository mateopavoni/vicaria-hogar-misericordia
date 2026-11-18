using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace Vicaria.Infrastructure.Pdf;

// Escritor común para los PDF que genera el backend (ficha, historial de medicación, resumen
// mensual de asistencia, informe de Cáritas): encabezado con color, títulos de sección, filas
// etiqueta/valor con columnas que no se pisan, corte de líneas, salto de página y pie numerado.
// Antes cada generador tenía su propio PdfWriter y solo uno cortaba líneas: un texto largo se
// salía de la hoja.
public sealed class PdfReportWriter : IDisposable
{
    public const string SystemName = "Vicaria | Nuestra Señora de la Misericordia";

    private const double Margin = 50;
    private const double HeaderHeight = 64;
    private const double BottomLimit = 60;
    private const double LabelGap = 14;

    private static readonly XColor Brand = XColor.FromArgb(41, 69, 133);
    private static readonly XColor Text = XColor.FromArgb(33, 38, 51);
    private static readonly XColor Muted = XColor.FromArgb(107, 115, 133);
    private static readonly XColor Stripe = XColor.FromArgb(240, 245, 252);
    private static readonly XColor Rule = XColor.FromArgb(199, 212, 237);
    private static readonly XColor AlertBackground = XColor.FromArgb(253, 236, 236);
    private static readonly XColor AlertText = XColor.FromArgb(160, 32, 32);

    private static readonly XFont TitleFont = new("Helvetica", 18, XFontStyleEx.Bold);
    private static readonly XFont HeaderFont = new("Helvetica", 11, XFontStyleEx.Bold);
    private static readonly XFont SectionFont = new("Helvetica", 13, XFontStyleEx.Bold);
    private static readonly XFont BodyFont = new("Helvetica", 10, XFontStyleEx.Regular);
    private static readonly XFont BoldFont = new("Helvetica", 10, XFontStyleEx.Bold);
    private static readonly XFont MetaFont = new("Helvetica", 9, XFontStyleEx.Regular);
    private static readonly XFont FooterFont = new("Helvetica", 8, XFontStyleEx.Regular);

    private readonly PdfDocument _document;
    private readonly string _footerText;
    private PdfPage _page;
    private XGraphics _graphics;
    private double _y;
    private int _rowIndex;

    public PdfReportWriter(string title, string? footerText = null)
    {
        _document = new PdfDocument();
        _document.Info.Title = title;
        _footerText = footerText ?? SystemName;
        _page = _document.AddPage();
        _graphics = XGraphics.FromPdfPage(_page);

        _graphics.DrawRectangle(new XSolidBrush(Brand), 0, 0, _page.Width.Point, HeaderHeight);
        _graphics.DrawString(Fit(SystemName, HeaderFont, ContentWidth), HeaderFont, XBrushes.White,
            new XPoint(Margin, 26), XStringFormats.TopLeft);
        _y = HeaderHeight + 22;

        foreach (var line in Wrap(title, TitleFont, ContentWidth))
        {
            DrawText(line, TitleFont, Brand, Margin, 24);
        }
        _y += 2;
    }

    private double ContentWidth => _page.Width.Point - Margin * 2;

    /// <summary>Línea secundaria bajo el título (fecha de generación, autor, período).</summary>
    public void Meta(string text)
    {
        foreach (var line in Wrap(text, MetaFont, ContentWidth))
        {
            DrawText(line, MetaFont, Muted, Margin, 13);
        }
    }

    /// <summary>Título de sección con línea de acento; nunca queda solo al pie de una página.</summary>
    public void Section(string title)
    {
        EnsureSpace(18 + 20 + 16 * 2);
        _y += 18;
        foreach (var line in Wrap(title, SectionFont, ContentWidth))
        {
            DrawText(line, SectionFont, Brand, Margin, 18);
        }
        _graphics.DrawRectangle(new XSolidBrush(Rule), Margin, _y + 1, ContentWidth, 1.2);
        _y += 7;
        _rowIndex = 0;
    }

    /// <summary>
    /// Ancho de columna de etiquetas para un conjunto de filas: el de la etiqueta más larga, con
    /// tope para que el valor siempre tenga lugar (si una etiqueta igual no entra, se corta).
    /// </summary>
    public double LabelWidthFor(IEnumerable<string> labels)
    {
        var widest = labels.Select(l => _graphics.MeasureString(l, BoldFont).Width).DefaultIfEmpty(0).Max();
        return Math.Min(Math.Max(widest + LabelGap, 110), ContentWidth * 0.6);
    }

    /// <summary>Fila etiqueta/valor con fondo alternado; etiqueta y valor se cortan por separado.</summary>
    public void Field(string label, string? value, double labelWidth)
    {
        var labelLines = Wrap(label, BoldFont, labelWidth - LabelGap);
        var valueLines = Wrap(string.IsNullOrWhiteSpace(value) ? "-" : value, BodyFont, ContentWidth - labelWidth - 8);
        var rows = Math.Max(labelLines.Count, valueLines.Count);
        const double rowLine = 14;
        var height = rows * rowLine + 6;

        EnsureSpace(height);
        if (_rowIndex++ % 2 == 0)
        {
            _graphics.DrawRectangle(new XSolidBrush(Stripe), Margin - 6, _y, ContentWidth + 12, height);
        }
        for (var r = 0; r < rows; r++)
        {
            var top = _y + 3 + r * rowLine;
            if (r < labelLines.Count)
            {
                _graphics.DrawString(labelLines[r], BoldFont, new XSolidBrush(Text), new XPoint(Margin, top), XStringFormats.TopLeft);
            }
            if (r < valueLines.Count)
            {
                _graphics.DrawString(valueLines[r], BodyFont, new XSolidBrush(Text), new XPoint(Margin + labelWidth, top), XStringFormats.TopLeft);
            }
        }
        _y += height;
    }

    /// <summary>Encabezado chico dentro de una sección (fecha y categoría de una observación, etc.).</summary>
    public void Heading(string text)
    {
        EnsureSpace(14 * 2 + 8);
        _y += 8;
        foreach (var line in Wrap(text, BoldFont, ContentWidth))
        {
            DrawText(line, BoldFont, Brand, Margin, 14);
        }
    }

    /// <summary>Párrafo de texto corrido, cortado al ancho de la página.</summary>
    public void Paragraph(string text, bool muted = false)
    {
        foreach (var line in Wrap(text, BodyFont, ContentWidth))
        {
            DrawText(line, BodyFont, muted ? Muted : Text, Margin, 14);
        }
    }

    /// <summary>Aviso destacado en rojo (tomas no administradas, datos de ejemplo).</summary>
    public void Alert(string text)
    {
        var lines = Wrap(text, BoldFont, ContentWidth - 16);
        var height = lines.Count * 14 + 10;
        EnsureSpace(height + 6);
        _y += 6;
        _graphics.DrawRectangle(new XSolidBrush(AlertBackground), Margin - 6, _y, ContentWidth + 12, height);
        var top = _y + 5;
        foreach (var line in lines)
        {
            _graphics.DrawString(line, BoldFont, new XSolidBrush(AlertText), new XPoint(Margin + 2, top), XStringFormats.TopLeft);
            top += 14;
        }
        _y += height;
    }

    public void Space(double points = 8) => _y += points;

    /// <summary>Hora de Córdoba para mostrar en los PDF (los datos se guardan en UTC).</summary>
    public static DateTime ToCordobaTime(DateTime utc)
    {
        var value = utc.Kind == DateTimeKind.Local ? utc.ToUniversalTime() : DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        foreach (var id in new[] { "America/Argentina/Cordoba", "Argentina Standard Time" })
        {
            try
            {
                return TimeZoneInfo.ConvertTimeFromUtc(value, TimeZoneInfo.FindSystemTimeZoneById(id));
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }
        return value.AddHours(-3);
    }

    public byte[] Save()
    {
        _graphics.Dispose();

        // pie con el número de página: se escribe al final porque recién ahí se conoce el total
        var total = _document.PageCount;
        for (var i = 0; i < total; i++)
        {
            var page = _document.Pages[i];
            using var g = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
            var width = page.Width.Point - Margin * 2;
            var footerY = page.Height.Point - 36;
            var pageLabel = $"Página {i + 1} de {total}";
            var pageLabelWidth = g.MeasureString(pageLabel, FooterFont).Width;

            g.DrawRectangle(new XSolidBrush(Rule), Margin, footerY - 6, width, 0.8);
            g.DrawString(Fit(_footerText, FooterFont, width - pageLabelWidth - 16, g), FooterFont, new XSolidBrush(Muted),
                new XPoint(Margin, footerY), XStringFormats.TopLeft);
            g.DrawString(pageLabel, FooterFont, new XSolidBrush(Muted),
                new XPoint(Margin + width - pageLabelWidth, footerY), XStringFormats.TopLeft);
        }

        using var stream = new MemoryStream();
        _document.Save(stream);
        return stream.ToArray();
    }

    public void Dispose()
    {
        _graphics.Dispose();
        _document.Dispose();
    }

    private void DrawText(string text, XFont font, XColor color, double x, double lineHeight)
    {
        EnsureSpace(lineHeight);
        _graphics.DrawString(text, font, new XSolidBrush(color), new XPoint(x, _y), XStringFormats.TopLeft);
        _y += lineHeight;
    }

    private void EnsureSpace(double height)
    {
        if (_y + height <= _page.Height.Point - BottomLimit)
        {
            return;
        }

        _graphics.Dispose();
        _page = _document.AddPage();
        _graphics = XGraphics.FromPdfPage(_page);
        _y = Margin;
        _rowIndex = 0;
    }

    // corta en palabras; una palabra más larga que la línea se parte en pedazos
    private List<string> Wrap(string text, XFont font, double maxWidth)
    {
        var result = new List<string>();
        foreach (var paragraph in (text ?? string.Empty).Replace("\r", string.Empty).Split('\n'))
        {
            var line = string.Empty;
            foreach (var rawWord in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var word = rawWord;
                var candidate = line.Length == 0 ? word : $"{line} {word}";
                if (Width(candidate, font) <= maxWidth)
                {
                    line = candidate;
                    continue;
                }
                if (line.Length > 0)
                {
                    result.Add(line);
                }
                while (Width(word, font) > maxWidth && word.Length > 1)
                {
                    var n = word.Length - 1;
                    while (n > 1 && Width(word[..n], font) > maxWidth) n--;
                    result.Add(word[..n]);
                    word = word[n..];
                }
                line = word;
            }
            result.Add(line);
        }
        return result;
    }

    private double Width(string text, XFont font) => _graphics.MeasureString(text, font).Width;

    private string Fit(string text, XFont font, double maxWidth, XGraphics? graphics = null)
    {
        var g = graphics ?? _graphics;
        if (g.MeasureString(text, font).Width <= maxWidth) return text;
        var n = text.Length;
        while (n > 0 && g.MeasureString(text[..n] + "...", font).Width > maxWidth) n--;
        return text[..n] + "...";
    }
}
