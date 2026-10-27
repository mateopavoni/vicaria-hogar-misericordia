namespace Vicaria.Application.PersonAttachments;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);
    Task<Stream?> GetFileAsync(string storedFileName, CancellationToken cancellationToken = default);
    Task<bool> DeleteFileAsync(string storedFileName, CancellationToken cancellationToken = default);
}