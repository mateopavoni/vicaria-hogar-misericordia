using Microsoft.AspNetCore.Hosting;
using Vicaria.Application.Backups;

namespace Vicaria.Infrastructure.Backups;

public class LocalBackupStorageService : IBackupStorageService
{
    private readonly string _storageDirectory;

    public LocalBackupStorageService(IWebHostEnvironment environment)
    {
        var basePath = environment.ContentRootPath;
        _storageDirectory = Path.Combine(basePath, "uploads", "backups");
        if (!Directory.Exists(_storageDirectory))
        {
            Directory.CreateDirectory(_storageDirectory);
        }
    }

    public async Task<StoredBackupFile> UploadAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(_storageDirectory, storedFileName);

        using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(fileStream, cancellationToken);

        var sizeBytes = new FileInfo(fullPath).Length;
        return new StoredBackupFile(storedFileName, sizeBytes);
    }

    public Task<Stream?> DownloadAsync(string externalKey, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_storageDirectory, externalKey);
        if (!File.Exists(fullPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string externalKey, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_storageDirectory, externalKey);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}
