namespace Vicaria.Application.Attendances;

public interface IAttendanceService
{
    Task<RegisterAttendanceResult> RegisterAsync(CreateAttendanceDto dto, Guid actorId, CancellationToken cancellationToken = default);
}