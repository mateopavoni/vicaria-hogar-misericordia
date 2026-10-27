namespace Vicaria.Application.Backups;

public record StoredBackupFile(string ExternalKey, long SizeBytes);

public interface IBackupStorageService
{
    Task<StoredBackupFile> UploadAsync(Stream content, string fileName, CancellationToken cancellationToken = default);
    Task<Stream?> DownloadAsync(string externalKey, CancellationToken cancellationToken = default);
    Task DeleteAsync(string externalKey, CancellationToken cancellationToken = default);
}
