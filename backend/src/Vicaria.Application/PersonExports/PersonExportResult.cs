namespace Vicaria.Application.PersonExports;

public enum PersonExportError
{
    PersonNotFound,
    SocialRecordNotFound,
    InvalidSections
}

public class PersonExportResult
{
    public bool Success { get; init; }
    public PersonExportDocument? Document { get; init; }
    public PersonExportError? Error { get; init; }
    public string? ErrorMessage { get; init; }

    public static PersonExportResult Ok(PersonExportDocument document) =>
        new() { Success = true, Document = document };

    public static PersonExportResult PersonNotFound() =>
        new() { Success = false, Error = PersonExportError.PersonNotFound, ErrorMessage = "La persona no existe." };

    public static PersonExportResult SocialRecordNotFound() =>
        new() { Success = false, Error = PersonExportError.SocialRecordNotFound, ErrorMessage = "La ficha no existe." };

    public static PersonExportResult InvalidSections() =>
        new() { Success = false, Error = PersonExportError.InvalidSections, ErrorMessage = "Las secciones exportadas no son válidas." };
}
