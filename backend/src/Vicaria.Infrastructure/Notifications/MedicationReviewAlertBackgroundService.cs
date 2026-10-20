using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vicaria.Application.Notifications;

namespace Vicaria.Infrastructure.Notifications;

public class MedicationReviewAlertBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MedicationReviewAlertBackgroundService> _logger;
    private readonly TimeSpan _period = TimeSpan.FromHours(24);

    public MedicationReviewAlertBackgroundService(IServiceScopeFactory scopeFactory, ILogger<MedicationReviewAlertBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_period);

        do
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IMedicationReviewAlertService>();
                await service.CheckAndNotifyAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar las alertas de revision de los esquemas de medicacion.");
            }
        }
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken));
    }
}
