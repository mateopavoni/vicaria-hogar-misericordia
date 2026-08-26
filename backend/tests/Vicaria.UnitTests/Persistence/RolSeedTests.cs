using Microsoft.EntityFrameworkCore;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.Persistence;

public class RolSeedTests
{
    private static VicariaDbContext CrearDbContext()
    {
        var options = new DbContextOptionsBuilder<VicariaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new VicariaDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public void SeedTieneLosCuatroRolesEsperados()
    {
        using var db = CrearDbContext();

        var nombres = db.Roles.Select(r => r.Nombre).ToList();

        Assert.Equal(4, nombres.Count);
        Assert.Contains(RolNombres.Referente, nombres);
        Assert.Contains(RolNombres.DirectoraDeCasona, nombres);
        Assert.Contains(RolNombres.Escucha, nombres);
        Assert.Contains(RolNombres.CoordinadorDeCasaConvivencia, nombres);
    }
}
