namespace Vicaria.Application.SocialRecords;

// SCRUM-23: la ficha se arma en PDF o vista de impresión en el navegador; este registro
// deja en la auditoría quién la exportó, cuándo y con qué secciones
// (POST /api/social-records/{id}/exports)
public record LogRecordExportDto(string Sections, string Format);

public enum LogRecordExportError
{
    NotFound
}

public class LogRecordExportResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public LogRecordExportError? Error { get; init; }

    public static LogRecordExportResult Ok() => new() { Success = true };

    public static LogRecordExportResult NotFound() =>
        new() { Success = false, Error = LogRecordExportError.NotFound, ErrorMessage = "La ficha no existe." };
}
