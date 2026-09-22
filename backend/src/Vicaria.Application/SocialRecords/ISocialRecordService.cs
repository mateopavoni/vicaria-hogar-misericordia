using Vicaria.Application.Persons;
using Vicaria.Domain.Entities;

namespace Vicaria.Application.SocialRecords;

public interface ISocialRecordService
{
    Task<CreateSocialRecordResult> CreateAsync(CreateSocialRecordDto dto, Guid actorId, CancellationToken cancellationToken = default);
    Task<List<SocialRecordSearchResultDto>> SearchAsync(string? query, PersonType? personTypeFilter = null, CancellationToken cancellationToken = default);
    Task<UpdateSocialRecordResult> UpdateAsync(Guid socialRecordId, UpdateSocialRecordDto dto, Guid actorId, CancellationToken cancellationToken = default);
    Task<int> CountByFilterAsync(FilterSocialRecordsDto filter, CancellationToken cancellationToken = default);
    Task<UpdatePersonTypeResult> UpdatePersonTypeAsync(Guid personId, UpdatePersonTypeDto dto, Guid actorId, CancellationToken cancellationToken = default);
    Task<UpdatePersonProfileStatusResult> UpdatePersonProfileStatusAsync(Guid personId,UpdatePersonProfileStatusDto dto,Guid actorId,CancellationToken cancellationToken = default);
}
