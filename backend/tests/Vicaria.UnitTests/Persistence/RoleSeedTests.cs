using Microsoft.EntityFrameworkCore;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.Persistence;

public class RoleSeedTests
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

        var names = db.Roles.Select(r => r.Name).ToList();

        Assert.Equal(4, names.Count);
        Assert.Contains(RoleNames.Referent, names);
        Assert.Contains(RoleNames.CasaConvivenciaDirector, names);
        Assert.Contains(RoleNames.Listener, names);
        Assert.Contains(RoleNames.CasaConvivenciaCoordinator, names);
    }
}
