namespace Vicaria.Application.Observations;

public interface IObservationService
{
    Task<CreateObservationResult> CreateObservationAsync(
        Guid personId,
        CreateObservationDto dto,
        Guid authorUserId,
        CancellationToken cancellationToken);

        Task<ObservationsTimelineResponseDto> GetTimelineAsync(
        Guid personId,
        GetObservationsFilterDto filters,
        CancellationToken cancellationToken = default);

    // exportacion de las observaciones filtradas a CSV (SCRUM-166)
    Task<string> ExportToCsvAsync(
        Guid personId,
        GetObservationsFilterDto filters,
        CancellationToken cancellationToken = default);
}