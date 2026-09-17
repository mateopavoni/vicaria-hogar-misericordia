namespace Vicaria.Application.Persons;

public interface IPersonInactivityService
{
    Task CheckAndProcessInactivityAsync(CancellationToken cancellationToken = default);
}