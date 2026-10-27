using Microsoft.EntityFrameworkCore;
using Vicaria.Application.PersonAttachments;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.Infrastructure.PersonAttachments;

public class PersonAttachmentService : IPersonAttachmentService
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".pdf"
    };

    private readonly VicariaDbContext _dbContext;
    private readonly IFileStorageService _fileStorageService;

    public PersonAttachmentService(VicariaDbContext dbContext, IFileStorageService fileStorageService)
    {
        _dbContext = dbContext;
        _fileStorageService = fileStorageService;
    }

    public async Task<UploadPersonAttachmentsResult> UploadAsync(Guid personId, IReadOnlyList<UploadPersonAttachmentItem> files, Guid actorId, CancellationToken cancellationToken = default)
    {
        var personExists = await _dbContext.People.AsNoTracking().AnyAsync(p => p.Id == personId, cancellationToken);
        if (!personExists)
        {
            return UploadPersonAttachmentsResult.PersonNotFound();
        }

        if (files is null || files.Count == 0)
        {
            return UploadPersonAttachmentsResult.NoFilesProvided();
        }

        foreach (var file in files)
        {
            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
            {
                return UploadPersonAttachmentsResult.InvalidFileType($"El archivo '{file.FileName}' no tiene un formato permitido. Solo se aceptan JPG, PNG y PDF.");
            }

            if (file.Length > MaxFileSizeBytes)
            {
                return UploadPersonAttachmentsResult.FileTooLarge($"El archivo '{file.FileName}' supera el tamaño máximo permitido de 10 MB.");
            }
        }

        var actor = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == actorId, cancellationToken);
        var actorName = actor is null ? string.Empty : $"{actor.FirstName} {actor.LastName}".Trim();

        var createdAttachments = new List<PersonAttachment>();
        var responseDtos = new List<PersonAttachmentDto>();

        foreach (var file in files)
        {
            var storedFileName = await _fileStorageService.SaveFileAsync(file.ContentStream, file.FileName, cancellationToken);

            var attachment = new PersonAttachment
            {
                Id = Guid.NewGuid(),
                PersonId = personId,
                OriginalFileName = Path.GetFileName(file.FileName),
                StoredFileName = storedFileName,
                ContentType = ResolveContentType(file.FileName, file.ContentType),
                FileSizeBytes = file.Length,
                UploadedAt = DateTime.UtcNow,
                UploadedByUserId = actorId
            };

            createdAttachments.Add(attachment);

            responseDtos.Add(new PersonAttachmentDto(
                attachment.Id,
                attachment.PersonId,
                attachment.OriginalFileName,
                attachment.ContentType,
                attachment.FileSizeBytes,
                ResolveIcon(attachment.ContentType),
                attachment.UploadedAt,
                attachment.UploadedByUserId,
                actorName
            ));
        }

        _dbContext.PersonAttachments.AddRange(createdAttachments);

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = $"Se adjuntaron {createdAttachments.Count} archivo(s) a la ficha",
            AffectedEntity = $"Person:{personId}",
            Date = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return UploadPersonAttachmentsResult.Ok(responseDtos);
    }

    public async Task<GetPersonAttachmentsResult> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var personExists = await _dbContext.People.AsNoTracking().AnyAsync(p => p.Id == personId, cancellationToken);
        if (!personExists)
        {
            return GetPersonAttachmentsResult.PersonNotFound();
        }

        var attachments = await _dbContext.PersonAttachments
            .AsNoTracking()
            .Include(a => a.UploadedBy)
            .Where(a => a.PersonId == personId)
            .OrderByDescending(a => a.UploadedAt)
            .Select(a => new PersonAttachmentDto(
                a.Id,
                a.PersonId,
                a.OriginalFileName,
                a.ContentType,
                a.FileSizeBytes,
                ResolveIcon(a.ContentType),
                a.UploadedAt,
                a.UploadedByUserId,
                a.UploadedBy != null ? $"{a.UploadedBy.FirstName} {a.UploadedBy.LastName}".Trim() : string.Empty
            ))
            .ToListAsync(cancellationToken);

        return GetPersonAttachmentsResult.Ok(attachments);
    }

    public async Task<GetPersonAttachmentFileResult> GetFileAsync(Guid personId, Guid attachmentId, CancellationToken cancellationToken = default)
    {
        var personExists = await _dbContext.People.AsNoTracking().AnyAsync(p => p.Id == personId, cancellationToken);
        if (!personExists)
        {
            return GetPersonAttachmentFileResult.PersonNotFound();
        }

        var attachment = await _dbContext.PersonAttachments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.PersonId == personId, cancellationToken);

        if (attachment is null)
        {
            return GetPersonAttachmentFileResult.AttachmentNotFound();
        }

        var stream = await _fileStorageService.GetFileAsync(attachment.StoredFileName, cancellationToken);
        if (stream is null)
        {
            return GetPersonAttachmentFileResult.FileNotFoundOnDisk();
        }

        return GetPersonAttachmentFileResult.Ok(new PersonAttachmentFileStreamDto(
            stream,
            attachment.ContentType,
            attachment.OriginalFileName
        ));
    }

    public async Task<DeletePersonAttachmentResult> DeleteAsync(Guid personId, Guid attachmentId, Guid actorId, CancellationToken cancellationToken = default)
    {
        var personExists = await _dbContext.People.AsNoTracking().AnyAsync(p => p.Id == personId, cancellationToken);
        if (!personExists)
        {
            return DeletePersonAttachmentResult.PersonNotFound();
        }

        var attachment = await _dbContext.PersonAttachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.PersonId == personId, cancellationToken);

        if (attachment is null)
        {
            return DeletePersonAttachmentResult.AttachmentNotFound();
        }

        _dbContext.PersonAttachments.Remove(attachment);

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = $"Archivo adjunto eliminado: {attachment.OriginalFileName}",
            AffectedEntity = $"PersonAttachment:{attachment.Id}",
            Date = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _fileStorageService.DeleteFileAsync(attachment.StoredFileName, cancellationToken);

        return DeletePersonAttachmentResult.Ok();
    }

    private static string ResolveContentType(string fileName, string? providedContentType)
    {
        if (!string.IsNullOrWhiteSpace(providedContentType) && providedContentType != "application/octet-stream")
        {
            return providedContentType;
        }

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
    }

    private static string ResolveIcon(string contentType)
    {
        if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return "image";
        }

        if (contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return "pdf";
        }

        return "file";
    }
}