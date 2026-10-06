namespace Vicaria.Application.Events;

public interface IEventService
{
    Task<IReadOnlyList<EventOccurrenceDto>> GetEventsByRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    Task<EventDetailDto?> GetEventByIdAsync(Guid id, CancellationToken cancellationToken = default);
}