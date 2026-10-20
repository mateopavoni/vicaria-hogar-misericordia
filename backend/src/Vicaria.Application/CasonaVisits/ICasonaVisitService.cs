using Vicaria.Application.Common;

namespace Vicaria.Application.CasonaVisits;

public interface ICasonaVisitService
{
    Task<CreateCasonaVisitResult> CreateAsync(
        CreateCasonaVisitDto dto,
        Guid actorId,
        CancellationToken cancellationToken);

    Task<UpdateCasonaVisitResult> UpdateAsync(
        Guid casonaVisitId,
        UpdateCasonaVisitDto dto,
        Guid actorId,
        CancellationToken cancellationToken);

        Task<ChangeCasonaVisitStatusResult> ChangeStatusAsync(
        Guid casonaVisitId,
        ChangeCasonaVisitStatusDto dto,
        Guid actorId,
        CancellationToken cancellationToken);

    Task<PagedResult<CasonaVisitListItemDto>> GetByRangeAsync(
        DateTime from,
        DateTime to,
        int page,
        CancellationToken cancellationToken);

        
}
