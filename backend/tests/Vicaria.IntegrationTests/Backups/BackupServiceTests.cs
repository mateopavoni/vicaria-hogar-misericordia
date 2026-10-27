using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Vicaria.Application.Backups;
using Vicaria.Domain.Entities;
using Vicaria.IntegrationTests.Auth;

namespace Vicaria.IntegrationTests.Backups;

// SCRUM-77: el SQL Server corre en un contenedor aparte, igual que en Docker y en un deploy
// real, así que el .bak se genera en otro disco y tiene que llegar a la API por la conexión
public class BackupServiceTests : IClassFixture<VicariaWebApplicationFactory>
{
    private readonly VicariaWebApplicationFactory _factory;

    public BackupServiceTests(VicariaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateDailyBackupAsync_ConSqlServerEnOtroContenedor_GuardaUnBakDescargable()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IBackupService>();
        var storage = scope.ServiceProvider.GetRequiredService<IBackupStorageService>();

        var backup = await service.CreateDailyBackupAsync();

        Assert.Equal(BackupStatus.Succeeded, backup.Status);
        Assert.True(backup.SizeBytes > 0);

        await using var file = await storage.DownloadAsync(backup.ExternalKey);
        Assert.NotNull(file);
        var header = new byte[4];
        await file.ReadExactlyAsync(header);
        // un .bak de SQL Server empieza con "TAPE" (formato MTF) o, si está comprimido
        // (todas las ediciones menos Express), con "MSSQLBAK"
        Assert.Contains(Encoding.ASCII.GetString(header), new[] { "TAPE", "MSSQ" });
    }
}
