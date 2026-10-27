namespace Vicaria.Application.Backups;

public interface IBackupService
{
    Task<GetBackupsResult> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<BackupDto?> GetLatestAsync(CancellationToken cancellationToken = default);
    Task<GetBackupFileResult> GetFileAsync(Guid id, Guid actorId, CancellationToken cancellationToken = default);
    Task<BackupDto> CreateDailyBackupAsync(CancellationToken cancellationToken = default);
}
