namespace Vicaria.Application.PersonAttachments;

public enum UploadPersonAttachmentsError
{
    PersonNotFound,
    NoFilesProvided,
    InvalidFileType,
    FileTooLarge
}

public class UploadPersonAttachmentsResult
{
    public bool Success { get; init; }
    public IReadOnlyList<PersonAttachmentDto>? Attachments { get; init; }
    public UploadPersonAttachmentsError? Error { get; init; }
    public string? ErrorMessage { get; init; }

    public static UploadPersonAttachmentsResult Ok(IReadOnlyList<PersonAttachmentDto> attachments) =>
        new() { Success = true, Attachments = attachments };

    public static UploadPersonAttachmentsResult PersonNotFound() =>
        new() { Success = false, Error = UploadPersonAttachmentsError.PersonNotFound, ErrorMessage = "La persona indicada no existe." };

    public static UploadPersonAttachmentsResult NoFilesProvided() =>
        new() { Success = false, Error = UploadPersonAttachmentsError.NoFilesProvided, ErrorMessage = "Debe proporcionar al menos un archivo para adjuntar." };

    public static UploadPersonAttachmentsResult InvalidFileType(string message) =>
        new() { Success = false, Error = UploadPersonAttachmentsError.InvalidFileType, ErrorMessage = message };

    public static UploadPersonAttachmentsResult FileTooLarge(string message) =>
        new() { Success = false, Error = UploadPersonAttachmentsError.FileTooLarge, ErrorMessage = message };
}

public enum GetPersonAttachmentsError
{
    PersonNotFound
}

public class GetPersonAttachmentsResult
{
    public bool Success { get; init; }
    public IReadOnlyList<PersonAttachmentDto>? Attachments { get; init; }
    public GetPersonAttachmentsError? Error { get; init; }
    public string? ErrorMessage { get; init; }

    public static GetPersonAttachmentsResult Ok(IReadOnlyList<PersonAttachmentDto> attachments) =>
        new() { Success = true, Attachments = attachments };

    public static GetPersonAttachmentsResult PersonNotFound() =>
        new() { Success = false, Error = GetPersonAttachmentsError.PersonNotFound, ErrorMessage = "La persona indicada no existe." };
}

public enum GetPersonAttachmentFileError
{
    PersonNotFound,
    AttachmentNotFound,
    FileNotFoundOnDisk
}

public class GetPersonAttachmentFileResult
{
    public bool Success { get; init; }
    public PersonAttachmentFileStreamDto? FileStream { get; init; }
    public GetPersonAttachmentFileError? Error { get; init; }
    public string? ErrorMessage { get; init; }

    public static GetPersonAttachmentFileResult Ok(PersonAttachmentFileStreamDto fileStream) =>
        new() { Success = true, FileStream = fileStream };

    public static GetPersonAttachmentFileResult PersonNotFound() =>
        new() { Success = false, Error = GetPersonAttachmentFileError.PersonNotFound, ErrorMessage = "La persona indicada no existe." };

    public static GetPersonAttachmentFileResult AttachmentNotFound() =>
        new() { Success = false, Error = GetPersonAttachmentFileError.AttachmentNotFound, ErrorMessage = "El archivo adjunto no existe para esta persona." };

    public static GetPersonAttachmentFileResult FileNotFoundOnDisk() =>
        new() { Success = false, Error = GetPersonAttachmentFileError.FileNotFoundOnDisk, ErrorMessage = "El archivo físico no fue encontrado en el almacenamiento." };
}

public enum DeletePersonAttachmentError
{
    PersonNotFound,
    AttachmentNotFound
}

public class DeletePersonAttachmentResult
{
    public bool Success { get; init; }
    public DeletePersonAttachmentError? Error { get; init; }
    public string? ErrorMessage { get; init; }

    public static DeletePersonAttachmentResult Ok() =>
        new() { Success = true };

    public static DeletePersonAttachmentResult PersonNotFound() =>
        new() { Success = false, Error = DeletePersonAttachmentError.PersonNotFound, ErrorMessage = "La persona indicada no existe." };

    public static DeletePersonAttachmentResult AttachmentNotFound() =>
        new() { Success = false, Error = DeletePersonAttachmentError.AttachmentNotFound, ErrorMessage = "El archivo adjunto no existe para esta persona." };
}