namespace Vicaria.Application.Attendances;

public interface IAttendanceInactivityService
{
    Task CheckAndProcessInactivityAsync(CancellationToken cancellationToken = default);
}