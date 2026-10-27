namespace Vicaria.Application.PersonAttachments;

public interface IPersonAttachmentService
{
    Task<UploadPersonAttachmentsResult> UploadAsync(Guid personId, IReadOnlyList<UploadPersonAttachmentItem> files, Guid actorId, CancellationToken cancellationToken = default);
    Task<GetPersonAttachmentsResult> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default);
    Task<GetPersonAttachmentFileResult> GetFileAsync(Guid personId, Guid attachmentId, CancellationToken cancellationToken = default);
    Task<DeletePersonAttachmentResult> DeleteAsync(Guid personId, Guid attachmentId, Guid actorId, CancellationToken cancellationToken = default);
}