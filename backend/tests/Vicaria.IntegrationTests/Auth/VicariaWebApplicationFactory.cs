using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.MsSql;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.IntegrationTests.Auth;

// levanta un SQL Server real en Docker para los tests, igual que en producción
public class VicariaWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // valores fijos de test para firmar/validar JWT sin depender de secrets locales
    public const string JwtKey = "test-signing-key-not-for-production-use-only-in-tests-1234567890";
    public const string JwtIssuer = "VicariaApi.Tests";
    public const string JwtAudience = "VicariaApi.Tests";

    private readonly MsSqlContainer _sqlServer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    async Task IAsyncLifetime.InitializeAsync() => await _sqlServer.StartAsync();

    async Task IAsyncLifetime.DisposeAsync() => await _sqlServer.StopAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = JwtKey,
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience
            });
        });

        builder.ConfigureServices(services =>
        {
            // AddDbContext encadena configuración si se llama dos veces: hay que sacar
            // también esto, no solo las options, o quedan dos providers registrados
            services.RemoveAll<DbContextOptions<VicariaDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<VicariaDbContext>>();
            services.AddDbContext<VicariaDbContext>(options => options.UseSqlServer(_sqlServer.GetConnectionString()));

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
            db.Database.EnsureCreated();
        });
    }
}
