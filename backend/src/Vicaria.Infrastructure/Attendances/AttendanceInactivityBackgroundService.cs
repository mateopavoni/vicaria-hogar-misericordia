using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vicaria.Application.Attendances;

namespace Vicaria.Infrastructure.Attendances;

public class AttendanceInactivityBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AttendanceInactivityBackgroundService> _logger;
    private readonly TimeSpan _period = TimeSpan.FromHours(24);

    public AttendanceInactivityBackgroundService(IServiceScopeFactory scopeFactory, ILogger<AttendanceInactivityBackgroundService> logger)
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
                var service = scope.ServiceProvider.GetRequiredService<IAttendanceInactivityService>();
                await service.CheckAndProcessInactivityAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar la inactividad automática por asistencia.");
            }
        }
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken));
    }
}