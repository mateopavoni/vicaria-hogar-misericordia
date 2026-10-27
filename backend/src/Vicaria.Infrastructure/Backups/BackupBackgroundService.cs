using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vicaria.Application.Backups;

namespace Vicaria.Infrastructure.Backups;

public class BackupBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly BackupOptions _options;
    private readonly ILogger<BackupBackgroundService> _logger;

    public BackupBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<BackupOptions> options,
        ILogger<BackupBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var firstDelay = TimeUntilNextRun(DateTime.UtcNow, _options.ScheduleHourUtc);
        await Task.Delay(firstDelay, stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));

        do
        {
            await RunBackupOnceAsync(stoppingToken);
        }
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunBackupOnceAsync(CancellationToken stoppingToken)
    {
        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IBackupService>();
                var backup = await service.CreateDailyBackupAsync(stoppingToken);

                if (backup.Status == Domain.Entities.BackupStatus.Succeeded)
                {
                    return;
                }

                _logger.LogWarning("El respaldo automático terminó con estado {Status} (intento {Attempt}/{Max}).", backup.Status, attempt, maxAttempts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar el respaldo automático (intento {Attempt}/{Max}).", attempt, maxAttempts);
            }

            if (attempt < maxAttempts)
            {
                var backoff = TimeSpan.FromMinutes(Math.Pow(2, attempt));
                await Task.Delay(backoff, stoppingToken);
            }
        }
    }

    private static TimeSpan TimeUntilNextRun(DateTime now, int scheduleHourUtc)
    {
        var hour = Math.Clamp(scheduleHourUtc, 0, 23);
        var next = new DateTime(now.Year, now.Month, now.Day, hour, 0, 0, DateTimeKind.Utc);
        if (next <= now)
        {
            next = next.AddDays(1);
        }

        return next - now;
    }
}
