namespace Vicaria.Application.SocialRecords;

// campos de la ficha que alimentan el informe de Cáritas (SCRUM-270/275): un valor del
// catálogo configurable por campo; null = dato pendiente. Se lee en el detalle de la
// ficha y se guarda con PUT /api/social-records/{id}/report-fields
public record SocialRecordReportFieldsDto(
    Guid? GenderId,
    Guid? HousingSituationId,
    Guid? OriginLocalityId,
    Guid? DocumentationStatusId,
    Guid? AttendanceReasonId);

public enum UpdateReportFieldsError
{
    NotFound,
    InvalidCatalogValue
}

public class UpdateReportFieldsResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public UpdateReportFieldsError? Error { get; init; }

    public static UpdateReportFieldsResult Ok() => new() { Success = true };

    public static UpdateReportFieldsResult NotFound() =>
        new() { Success = false, Error = UpdateReportFieldsError.NotFound, ErrorMessage = "La ficha no existe." };

    public static UpdateReportFieldsResult InvalidCatalogValue(string message) =>
        new() { Success = false, Error = UpdateReportFieldsError.InvalidCatalogValue, ErrorMessage = message };
}

// campo del informe de Cáritas que la ficha todavía no tiene cargado (SCRUM-273):
// Field es la clave estable (gender, housing, origin, document, mainReason) y Label el
// nombre que ve el usuario
public record PendingReportFieldDto(string Field, string Label);
