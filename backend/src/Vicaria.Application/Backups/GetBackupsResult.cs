using Vicaria.Application.Common;

namespace Vicaria.Application.Backups;

public enum GetBackupsError
{
    InvalidPage
}

public class GetBackupsResult
{
    public bool Success { get; init; }
    public PagedResult<BackupDto>? Page { get; init; }
    public GetBackupsError? Error { get; init; }
    public string? ErrorMessage { get; init; }

    public static GetBackupsResult Ok(PagedResult<BackupDto> page) =>
        new() { Success = true, Page = page };

    public static GetBackupsResult InvalidPage() =>
        new() { Success = false, Error = GetBackupsError.InvalidPage, ErrorMessage = "La página solicitada no es válida." };
}
