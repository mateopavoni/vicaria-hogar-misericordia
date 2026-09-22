namespace Vicaria.Application.Observations;

public interface IObservationService
{
    Task<CreateObservationResult> CreateObservationAsync(
        Guid personId,
        CreateObservationDto dto,
        Guid authorUserId,
        CancellationToken cancellationToken);
}