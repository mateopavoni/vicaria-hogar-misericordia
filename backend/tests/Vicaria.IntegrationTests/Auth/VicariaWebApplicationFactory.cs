using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.IntegrationTests.Auth;

// levanta un Postgres real en Docker para los tests, igual que en producción
public class VicariaWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    async Task IAsyncLifetime.InitializeAsync() => await _postgres.StartAsync();

    async Task IAsyncLifetime.DisposeAsync() => await _postgres.StopAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // AddDbContext encadena configuración si se llama dos veces: hay que sacar
            // también esto, no solo las options, o quedan dos providers registrados
            services.RemoveAll<DbContextOptions<VicariaDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<VicariaDbContext>>();
            services.AddDbContext<VicariaDbContext>(options => options.UseNpgsql(_postgres.GetConnectionString()));

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<VicariaDbContext>();
            db.Database.EnsureCreated();
        });
    }
}
