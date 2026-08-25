using Microsoft.EntityFrameworkCore;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.Auth;

public class RolPermissionSeedTests
{
    [Fact]
    public async Task DirectoraDeCasona_TienePermisosDeFichasYAgenda()
    {
        var options = new DbContextOptionsBuilder<VicariaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new VicariaDbContext(options);
        db.Database.EnsureCreated();

        var directora = await db.Roles.SingleAsync(r => r.Nombre == RolNombres.DirectoraDeCasona);
        var codigos = await db.RolPermissions
            .Where(rp => rp.RolId == directora.Id)
            .Join(db.Permissions, rp => rp.PermissionId, p => p.Id, (rp, p) => p.Codigo)
            .ToListAsync();

        Assert.Equal(3, codigos.Count);
        Assert.Contains(PermissionNombres.VerFichasResidentesCasaConvivencia, codigos);
        Assert.Contains(PermissionNombres.CargarObservacionesResidentes, codigos);
        Assert.Contains(PermissionNombres.VerAgendaMedicamentos, codigos);
    }
}
