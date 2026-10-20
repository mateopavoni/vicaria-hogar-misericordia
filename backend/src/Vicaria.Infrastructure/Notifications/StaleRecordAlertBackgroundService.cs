using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vicaria.Application.Notifications;

namespace Vicaria.Infrastructure.Notifications;

public class StaleRecordAlertBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StaleRecordAlertBackgroundService> _logger;
    private readonly TimeSpan _period = TimeSpan.FromHours(24);

    public StaleRecordAlertBackgroundService(IServiceScopeFactory scopeFactory, ILogger<StaleRecordAlertBackgroundService> logger)
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
                var service = scope.ServiceProvider.GetRequiredService<IStaleRecordAlertService>();
                await service.CheckAndNotifyAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar las alertas de fichas sin observaciones.");
            }
        }
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken));
    }
}
