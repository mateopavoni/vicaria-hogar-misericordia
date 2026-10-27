using Microsoft.AspNetCore.Hosting;
using Vicaria.Application.PersonAttachments;

namespace Vicaria.Infrastructure.PersonAttachments;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _storageDirectory;

    public LocalFileStorageService(IWebHostEnvironment environment)
    {
        var basePath = environment.ContentRootPath;
        _storageDirectory = Path.Combine(basePath, "uploads", "person_attachments");
        if (!Directory.Exists(_storageDirectory))
        {
            Directory.CreateDirectory(_storageDirectory);
        }
    }

    public async Task<string> SaveFileAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(_storageDirectory, storedFileName);

        using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await stream.CopyToAsync(fileStream, cancellationToken);

        return storedFileName;
    }

    public Task<Stream?> GetFileAsync(string storedFileName, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_storageDirectory, storedFileName);
        if (!File.Exists(fullPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<Stream?>(stream);
    }

    public Task<bool> DeleteFileAsync(string storedFileName, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_storageDirectory, storedFileName);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }
}