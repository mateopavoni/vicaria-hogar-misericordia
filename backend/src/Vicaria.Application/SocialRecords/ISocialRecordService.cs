namespace Vicaria.Application.SocialRecords;

public interface ISocialRecordService
{
    Task<CreateSocialRecordResult> CreateAsync(CreateSocialRecordDto dto, Guid actorId, CancellationToken cancellationToken = default);
    Task<List<SocialRecordSearchResultDto>> SearchAsync(string? query, CancellationToken cancellationToken = default);
}
