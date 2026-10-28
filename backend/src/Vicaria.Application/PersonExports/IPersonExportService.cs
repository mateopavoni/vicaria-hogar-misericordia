namespace Vicaria.Application.PersonExports;

// valores válidos del query param sections (términos del negocio en español,
// igual que LogRecordExportDtoValidator.Sections del POST /exports existente)
public static class PersonExportSections
{
    public const string PersonalData = "datos";
    public const string DataWithObservations = "datos-observaciones";
    public const string All = "todo";

    public static readonly IReadOnlyList<string> Allowed = [PersonalData, DataWithObservations, All];
}

public interface IPersonExportService
{
    Task<PersonExportResult> GetExportAsync(Guid personId, string sections, Guid actorId, CancellationToken cancellationToken = default);
}
