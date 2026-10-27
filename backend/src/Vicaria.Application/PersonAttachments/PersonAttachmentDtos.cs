namespace Vicaria.Application.PersonAttachments;

public record PersonAttachmentDto(
    Guid Id,
    Guid PersonId,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string FileTypeIcon,
    DateTime UploadedAt,
    Guid UploadedByUserId,
    string UploadedByName
);

public record PersonAttachmentFileStreamDto(
    Stream Stream,
    string ContentType,
    string FileName
);

public record UploadPersonAttachmentItem(
    string FileName,
    string ContentType,
    long Length,
    Stream ContentStream
);