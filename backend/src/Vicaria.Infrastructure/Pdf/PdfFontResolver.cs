using PdfSharp.Fonts;

namespace Vicaria.Infrastructure.Pdf;

// PDFsharp no trae fuentes: se busca Arial en Windows y Liberation Sans en Linux (se instala
// en el Dockerfile). Los generadores piden "Helvetica", que no existe como archivo.
public class PdfFontResolver : IFontResolver
{
    private const string Regular = "VicariaSans-Regular";
    private const string Bold = "VicariaSans-Bold";

    private static readonly string FontsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

    private static readonly string[] RegularPaths =
    {
        Path.Combine(FontsFolder, "arial.ttf"),
        "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf",
        "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
        "/System/Library/Fonts/Supplemental/Arial.ttf"
    };

    private static readonly string[] BoldPaths =
    {
        Path.Combine(FontsFolder, "arialbd.ttf"),
        "/usr/share/fonts/truetype/liberation/LiberationSans-Bold.ttf",
        "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
        "/System/Library/Fonts/Supplemental/Arial Bold.ttf"
    };

    public static void Register()
    {
        GlobalFontSettings.FontResolver ??= new PdfFontResolver();
    }

    public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        return new FontResolverInfo(isBold ? Bold : Regular);
    }

    public byte[]? GetFont(string faceName)
    {
        var candidates = faceName == Bold ? BoldPaths : RegularPaths;
        var path = candidates.FirstOrDefault(File.Exists)
            ?? throw new InvalidOperationException("No hay una fuente instalada para generar PDFs (se espera Liberation Sans).");
        return File.ReadAllBytes(path);
    }
}
