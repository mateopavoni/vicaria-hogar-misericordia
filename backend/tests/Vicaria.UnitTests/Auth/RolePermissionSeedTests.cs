using Microsoft.EntityFrameworkCore;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.Auth;

public class RolePermissionSeedTests
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
        var codes = await db.RolePermissions
            .Where(rp => rp.RolId == directora.Id)
            .Join(db.Permissions, rp => rp.PermissionId, p => p.Id, (rp, p) => p.Code)
            .ToListAsync();

        Assert.Equal(3, codes.Count);
        Assert.Contains(PermissionNames.ViewCasaConvivenciaResidentRecords, codes);
        Assert.Contains(PermissionNames.LoadResidentObservations, codes);
        Assert.Contains(PermissionNames.ViewMedicationSchedule, codes);
    }
}
