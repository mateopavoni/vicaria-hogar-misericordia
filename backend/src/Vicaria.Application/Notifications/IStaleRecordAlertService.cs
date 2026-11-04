namespace Vicaria.Application.Notifications;

public interface IStaleRecordAlertService
{
    // / RF-19: avisa a los Referentes de las fichas activas sin observaciones en 30 días
    Task<int> CheckAndNotifyAsync(CancellationToken cancellationToken = default);
}
