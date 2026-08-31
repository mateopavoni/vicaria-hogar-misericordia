using Microsoft.EntityFrameworkCore;
using Vicaria.Application.Auth;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Auth;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.Auth;

public class AuthServiceApprovalTests
{
    private static VicariaDbContext CrearDbContext()
    {
        var options = new DbContextOptionsBuilder<VicariaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new VicariaDbContext(options);
        db.Database.EnsureCreated(); // necesario para que se apliquen los roles sembrados con HasData
        return db;
    }

    private static User CrearUsuarioPendiente(VicariaDbContext db)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Ana",
            LastName = "Perez",
            Email = $"{Guid.NewGuid()}@mail.com",
            PasswordHash = "hash",
            Status = UserStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    [Fact]
    public async Task GetPendingUsersAsync_DevuelveSoloLosPendientes()
    {
        using var db = CrearDbContext();
        var pendiente = CrearUsuarioPendiente(db);
        var activo = CrearUsuarioPendiente(db);
        activo.Status = UserStatus.Active;
        db.SaveChanges();

        var service = new AuthService(db);
        var resultado = await service.GetPendingUsersAsync();

        var unico = Assert.Single(resultado.Items);
        Assert.Equal(pendiente.Id, unico.Id);
    }

    [Fact]
    public async Task GetActiveUsersAsync_Pagina()
    {
        using var db = CrearDbContext();
        for (var i = 0; i < 12; i++)
        {
            CrearUsuarioPendiente(db).Status = UserStatus.Active;
        }
        db.SaveChanges();

        var service = new AuthService(db);
        var pagina2 = await service.GetActiveUsersAsync(page: 2);

        Assert.Equal(12, pagina2.Total);
        Assert.Equal(2, pagina2.TotalPages);
        Assert.Equal(2, pagina2.Items.Count);
    }

    [Fact]
    public async Task GetPendingUsersAsync_FiltraPorRangoDeFecha()
    {
        using var db = CrearDbContext();
        var viejo = CrearUsuarioPendiente(db);
        viejo.CreatedAt = DateTime.UtcNow.AddDays(-10);
        var reciente = CrearUsuarioPendiente(db);
        db.SaveChanges();

        var service = new AuthService(db);
        var resultado = await service.GetPendingUsersAsync(dateFrom: DateTime.UtcNow.AddDays(-1));

        var unico = Assert.Single(resultado.Items);
        Assert.Equal(reciente.Id, unico.Id);
    }

    [Fact]
    public async Task ApproveUserAsync_ConDatosValidos_AsignaRolYCambiaEstado()
    {
        using var db = CrearDbContext();
        var user = CrearUsuarioPendiente(db);
        var roleId = db.Roles.First().Id;
        var actorId = Guid.NewGuid();
        var service = new AuthService(db);

        var resultado = await service.ApproveUserAsync(user.Id, new ApproveUserDto(roleId), actorId);

        Assert.True(resultado.Success);
        var actualizado = await db.Users.FindAsync(user.Id);
        Assert.Equal(UserStatus.Active, actualizado!.Status);
        Assert.Equal(roleId, actualizado.RoleId);
    }

    [Fact]
    public async Task ApproveUserAsync_RegistraAuditLog()
    {
        using var db = CrearDbContext();
        var user = CrearUsuarioPendiente(db);
        var roleId = db.Roles.First().Id;
        var actorId = Guid.NewGuid();
        var service = new AuthService(db);

        await service.ApproveUserAsync(user.Id, new ApproveUserDto(roleId), actorId);

        var log = Assert.Single(db.AuditLogs);
        Assert.Equal(actorId, log.UserId);
        Assert.Contains(user.Id.ToString(), log.AffectedEntity);
    }

    [Fact]
    public async Task ApproveUserAsync_UsuarioNoExiste_RetornaUsuarioNoEncontrado()
    {
        using var db = CrearDbContext();
        var roleId = db.Roles.First().Id;
        var service = new AuthService(db);

        var resultado = await service.ApproveUserAsync(Guid.NewGuid(), new ApproveUserDto(roleId), Guid.NewGuid());

        Assert.False(resultado.Success);
        Assert.Equal(ApproveUserError.UserNotFound, resultado.Error);
    }

    [Fact]
    public async Task ApproveUserAsync_RolNoExiste_RetornaRolInvalido()
    {
        using var db = CrearDbContext();
        var user = CrearUsuarioPendiente(db);
        var service = new AuthService(db);

        var resultado = await service.ApproveUserAsync(user.Id, new ApproveUserDto(Guid.NewGuid()), Guid.NewGuid());

        Assert.False(resultado.Success);
        Assert.Equal(ApproveUserError.InvalidRole, resultado.Error);
    }

    [Fact]
    public async Task ApproveUserAsync_UsuarioYaAprobado_RetornaEstadoInvalido()
    {
        using var db = CrearDbContext();
        var user = CrearUsuarioPendiente(db);
        var roleId = db.Roles.First().Id;
        var service = new AuthService(db);
        await service.ApproveUserAsync(user.Id, new ApproveUserDto(roleId), Guid.NewGuid());

        var resultado = await service.ApproveUserAsync(user.Id, new ApproveUserDto(roleId), Guid.NewGuid());

        Assert.False(resultado.Success);
        Assert.Equal(ApproveUserError.InvalidState, resultado.Error);
    }

    [Fact]
    public async Task RejectUserAsync_ConMotivo_CambiaEstadoARechazado()
    {
        using var db = CrearDbContext();
        var user = CrearUsuarioPendiente(db);
        var service = new AuthService(db);

        var resultado = await service.RejectUserAsync(user.Id, new RejectUserDto("no cumple los requisitos"), Guid.NewGuid());

        Assert.True(resultado.Success);
        var actualizado = await db.Users.FindAsync(user.Id);
        Assert.Equal(UserStatus.Rejected, actualizado!.Status);
    }

    [Fact]
    public async Task RejectUserAsync_RegistraAuditLogConMotivo()
    {
        using var db = CrearDbContext();
        var user = CrearUsuarioPendiente(db);
        var service = new AuthService(db);

        await service.RejectUserAsync(user.Id, new RejectUserDto("no cumple los requisitos"), Guid.NewGuid());

        var log = Assert.Single(db.AuditLogs);
        Assert.Contains("no cumple los requisitos", log.Action);
    }

    [Fact]
    public async Task RejectUserAsync_UsuarioNoExiste_RetornaUsuarioNoEncontrado()
    {
        using var db = CrearDbContext();
        var service = new AuthService(db);

        var resultado = await service.RejectUserAsync(Guid.NewGuid(), new RejectUserDto("motivo"), Guid.NewGuid());

        Assert.False(resultado.Success);
        Assert.Equal(RejectUserError.UserNotFound, resultado.Error);
    }

    [Fact]
    public async Task RejectUserAsync_UsuarioYaRechazado_RetornaEstadoInvalido()
    {
        using var db = CrearDbContext();
        var user = CrearUsuarioPendiente(db);
        var service = new AuthService(db);
        await service.RejectUserAsync(user.Id, new RejectUserDto("motivo"), Guid.NewGuid());

        var resultado = await service.RejectUserAsync(user.Id, new RejectUserDto("otro motivo"), Guid.NewGuid());

        Assert.False(resultado.Success);
        Assert.Equal(RejectUserError.InvalidState, resultado.Error);
    }
}
