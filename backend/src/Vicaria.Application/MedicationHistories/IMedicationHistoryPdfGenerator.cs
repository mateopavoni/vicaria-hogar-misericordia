namespace Vicaria.Application.MedicationHistories;

public interface IMedicationHistoryPdfGenerator
{
    byte[] GeneratePersonHistoryPdf(MedicationHistoryPdfDocument document);
}
