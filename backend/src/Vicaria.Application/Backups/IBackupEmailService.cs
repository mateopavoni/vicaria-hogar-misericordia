namespace Vicaria.Application.Backups;

public interface IBackupEmailService
{
    Task SendBackupFailureAsync(string errorMessage, DateTime occurredAt, CancellationToken cancellationToken = default);
}
