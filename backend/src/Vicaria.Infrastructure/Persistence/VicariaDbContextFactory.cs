using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Vicaria.Infrastructure.Persistence;

public class VicariaDbContextFactory : IDesignTimeDbContextFactory<VicariaDbContext>
{
    public VicariaDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../Vicaria.Api"))
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddJsonFile("appsettings.Development.local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("VicariaDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("No se encontró la connection string 'VicariaDb' para las herramientas de diseño de EF Core.");
        }

        var options = new DbContextOptionsBuilder<VicariaDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new VicariaDbContext(options);
    }
}