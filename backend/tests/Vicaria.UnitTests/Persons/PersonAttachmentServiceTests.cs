using System.Text;
using Microsoft.EntityFrameworkCore;
using Vicaria.Application.PersonAttachments;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.PersonAttachments;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.Persons;

public class InMemoryFileStorageService : IFileStorageService
{
    private readonly Dictionary<string, byte[]> _files = new();

    public Task<string> SaveFileAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var storedName = $"{Guid.NewGuid():N}{Path.GetExtension(fileName)}";
        _files[storedName] = ms.ToArray();
        return Task.FromResult(storedName);
    }

    public Task<Stream?> GetFileAsync(string storedFileName, CancellationToken cancellationToken = default)
    {
        if (_files.TryGetValue(storedFileName, out var bytes))
        {
            return Task.FromResult<Stream?>(new MemoryStream(bytes));
        }
        return Task.FromResult<Stream?>(null);
    }

    public Task<bool> DeleteFileAsync(string storedFileName, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_files.Remove(storedFileName));
    }
}

public class PersonAttachmentServiceTests
{
    private static VicariaDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<VicariaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new VicariaDbContext(options);
    }

    [Fact]
    public async Task UploadAsync_WhenValidFiles_SavesAttachmentsAndAuditLog()
    {
        using var context = CreateInMemoryDbContext();
        var storage = new InMemoryFileStorageService();
        var service = new PersonAttachmentService(context, storage);

        var person = new Person { Id = Guid.NewGuid(), FirstName = "Raúl" };
        var user = new User { Id = Guid.NewGuid(), FirstName = "Lucía", LastName = "Referente" };
        context.People.Add(person);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var stream1 = new MemoryStream(Encoding.UTF8.GetBytes("fake content 1"));
        var stream2 = new MemoryStream(Encoding.UTF8.GetBytes("fake content 2"));
        var files = new List<UploadPersonAttachmentItem>
        {
            new("receta.jpg", "image/jpeg", stream1.Length, stream1),
            new("informe.pdf", "application/pdf", stream2.Length, stream2)
        };

        var result = await service.UploadAsync(person.Id, files, user.Id);

        Assert.True(result.Success);
        Assert.NotNull(result.Attachments);
        Assert.Equal(2, result.Attachments.Count);
        Assert.Equal("image", result.Attachments[0].FileTypeIcon);
        Assert.Equal("pdf", result.Attachments[1].FileTypeIcon);

        var dbAttachments = await context.PersonAttachments.Where(a => a.PersonId == person.Id).ToListAsync();
        Assert.Equal(2, dbAttachments.Count);

        var auditLog = await context.AuditLogs.FirstOrDefaultAsync(a => a.Action.Contains("Se adjuntaron 2 archivo(s)"));
        Assert.NotNull(auditLog);
        Assert.Equal(user.Id, auditLog.UserId);
    }

    [Fact]
    public async Task UploadAsync_WhenInvalidExtension_ReturnsInvalidFileType()
    {
        using var context = CreateInMemoryDbContext();
        var storage = new InMemoryFileStorageService();
        var service = new PersonAttachmentService(context, storage);

        var person = new Person { Id = Guid.NewGuid(), FirstName = "Raúl" };
        context.People.Add(person);
        await context.SaveChangesAsync();

        var stream = new MemoryStream(Encoding.UTF8.GetBytes("test"));
        var files = new List<UploadPersonAttachmentItem>
        {
            new("malware.exe", "application/x-msdownload", stream.Length, stream)
        };

        var result = await service.UploadAsync(person.Id, files, Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal(UploadPersonAttachmentsError.InvalidFileType, result.Error);
    }

    [Fact]
    public async Task UploadAsync_WhenExceeds10Mb_ReturnsFileTooLarge()
    {
        using var context = CreateInMemoryDbContext();
        var storage = new InMemoryFileStorageService();
        var service = new PersonAttachmentService(context, storage);

        var person = new Person { Id = Guid.NewGuid(), FirstName = "Raúl" };
        context.People.Add(person);
        await context.SaveChangesAsync();

        var stream = new MemoryStream();
        var largeSize = (10 * 1024 * 1024) + 1;
        var files = new List<UploadPersonAttachmentItem>
        {
            new("gran_foto.png", "image/png", largeSize, stream)
        };

        var result = await service.UploadAsync(person.Id, files, Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal(UploadPersonAttachmentsError.FileTooLarge, result.Error);
    }

    [Fact]
    public async Task DeleteAsync_WhenAttachmentExists_DeletesFromDbAndStorageAndCreatesAuditLog()
    {
        using var context = CreateInMemoryDbContext();
        var storage = new InMemoryFileStorageService();
        var service = new PersonAttachmentService(context, storage);

        var actorId = Guid.NewGuid();
        var person = new Person { Id = Guid.NewGuid(), FirstName = "Raúl" };
        context.People.Add(person);

        var stream = new MemoryStream(Encoding.UTF8.GetBytes("contenido"));
        var storedName = await storage.SaveFileAsync(stream, "dni.pdf");

        var attachment = new PersonAttachment
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            OriginalFileName = "dni.pdf",
            StoredFileName = storedName,
            ContentType = "application/pdf",
            FileSizeBytes = 100,
            UploadedAt = DateTime.UtcNow,
            UploadedByUserId = actorId
        };
        context.PersonAttachments.Add(attachment);
        await context.SaveChangesAsync();

        var result = await service.DeleteAsync(person.Id, attachment.Id, actorId);

        Assert.True(result.Success);

        var existsInDb = await context.PersonAttachments.AnyAsync(a => a.Id == attachment.Id);
        Assert.False(existsInDb);

        var streamFromStorage = await storage.GetFileAsync(storedName);
        Assert.Null(streamFromStorage);

        var auditLog = await context.AuditLogs.FirstOrDefaultAsync(a => a.Action.Contains("Archivo adjunto eliminado"));
        Assert.NotNull(auditLog);
        Assert.Equal(actorId, auditLog.UserId);
    }
}