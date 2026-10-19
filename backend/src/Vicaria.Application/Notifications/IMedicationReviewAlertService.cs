namespace Vicaria.Application.Notifications;

public interface IMedicationReviewAlertService
{
    // SCRUM-249: alerta esquemas vigentes de residentes con más de 30 días sin revisión,
    // y el recordatorio mensual el día 1. Devuelve la cantidad de notificaciones creadas.
    Task<int> CheckAndNotifyAsync(CancellationToken cancellationToken = default);
}
