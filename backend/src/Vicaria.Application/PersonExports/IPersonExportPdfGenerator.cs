namespace Vicaria.Application.PersonExports;

public interface IPersonExportPdfGenerator
{
    byte[] GenerateExportPdf(PersonExportDocument document);
}
