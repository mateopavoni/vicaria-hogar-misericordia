using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

    private static IConfiguration CrearConfiguration() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "test-signing-key-not-for-production-use-only-in-tests-1234567890",
            ["Jwt:Issuer"] = "VicariaApi.Tests",
            ["Jwt:Audience"] = "VicariaApi.Tests",
            ["Jwt:ExpirationMinutes"] = "1440",
            ["Jwt:RefreshTokenExpirationDays"] = "7"
        })
        .Build();

    private static Usuario CrearUsuarioPendiente(VicariaDbContext db)
    {
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Nombre = "Ana",
            Apellido = "Perez",
            Email = $"{Guid.NewGuid()}@mail.com",
            PasswordHash = "hash",
            Estado = EstadoUsuario.Pending,
            CreatedAt = DateTime.UtcNow
        };
        db.Usuarios.Add(usuario);
        db.SaveChanges();
        return usuario;
    }

    [Fact]
    public async Task GetPendingUsersAsync_DevuelveSoloLosPendientes()
    {
        using var db = CrearDbContext();
        var pendiente = CrearUsuarioPendiente(db);
        var activo = CrearUsuarioPendiente(db);
        activo.Estado = EstadoUsuario.Active;
        db.SaveChanges();

        var service = new AuthService(db, CrearConfiguration());
        var resultado = await service.GetPendingUsersAsync();

        var unico = Assert.Single(resultado);
        Assert.Equal(pendiente.Id, unico.Id);
    }

    [Fact]
    public async Task ApproveUserAsync_ConDatosValidos_AsignaRolYCambiaEstado()
    {
        using var db = CrearDbContext();
        var usuario = CrearUsuarioPendiente(db);
        var rolId = db.Roles.First().Id;
        var actorId = Guid.NewGuid();
        var service = new AuthService(db, CrearConfiguration());

        var resultado = await service.ApproveUserAsync(usuario.Id, new ApproveUserDto(rolId), actorId);

        Assert.True(resultado.Success);
        var actualizado = await db.Usuarios.FindAsync(usuario.Id);
        Assert.Equal(EstadoUsuario.Active, actualizado!.Estado);
        Assert.Equal(rolId, actualizado.RolId);
    }

    [Fact]
    public async Task ApproveUserAsync_RegistraAuditLog()
    {
        using var db = CrearDbContext();
        var usuario = CrearUsuarioPendiente(db);
        var rolId = db.Roles.First().Id;
        var actorId = Guid.NewGuid();
        var service = new AuthService(db, CrearConfiguration());

        await service.ApproveUserAsync(usuario.Id, new ApproveUserDto(rolId), actorId);

        var log = Assert.Single(db.AuditLogs);
        Assert.Equal(actorId, log.UsuarioId);
        Assert.Contains(usuario.Id.ToString(), log.EntidadAfectada);
    }

    [Fact]
    public async Task ApproveUserAsync_UsuarioNoExiste_RetornaUsuarioNoEncontrado()
    {
        using var db = CrearDbContext();
        var rolId = db.Roles.First().Id;
        var service = new AuthService(db, CrearConfiguration());

        var resultado = await service.ApproveUserAsync(Guid.NewGuid(), new ApproveUserDto(rolId), Guid.NewGuid());

        Assert.False(resultado.Success);
        Assert.Equal(ApproveUserError.UserNotFound, resultado.Error);
    }

    [Fact]
    public async Task ApproveUserAsync_RolNoExiste_RetornaRolInvalido()
    {
        using var db = CrearDbContext();
        var usuario = CrearUsuarioPendiente(db);
        var service = new AuthService(db, CrearConfiguration());

        var resultado = await service.ApproveUserAsync(usuario.Id, new ApproveUserDto(Guid.NewGuid()), Guid.NewGuid());

        Assert.False(resultado.Success);
        Assert.Equal(ApproveUserError.InvalidRole, resultado.Error);
    }

    [Fact]
    public async Task ApproveUserAsync_UsuarioYaAprobado_RetornaEstadoInvalido()
    {
        using var db = CrearDbContext();
        var usuario = CrearUsuarioPendiente(db);
        var rolId = db.Roles.First().Id;
        var service = new AuthService(db, CrearConfiguration());
        await service.ApproveUserAsync(usuario.Id, new ApproveUserDto(rolId), Guid.NewGuid());

        var resultado = await service.ApproveUserAsync(usuario.Id, new ApproveUserDto(rolId), Guid.NewGuid());

        Assert.False(resultado.Success);
        Assert.Equal(ApproveUserError.InvalidState, resultado.Error);
    }

    [Fact]
    public async Task RejectUserAsync_ConMotivo_CambiaEstadoARechazado()
    {
        using var db = CrearDbContext();
        var usuario = CrearUsuarioPendiente(db);
        var service = new AuthService(db, CrearConfiguration());

        var resultado = await service.RejectUserAsync(usuario.Id, new RejectUserDto("no cumple los requisitos"), Guid.NewGuid());

        Assert.True(resultado.Success);
        var actualizado = await db.Usuarios.FindAsync(usuario.Id);
        Assert.Equal(EstadoUsuario.Rejected, actualizado!.Estado);
    }

    [Fact]
    public async Task RejectUserAsync_RegistraAuditLogConMotivo()
    {
        using var db = CrearDbContext();
        var usuario = CrearUsuarioPendiente(db);
        var service = new AuthService(db, CrearConfiguration());

        await service.RejectUserAsync(usuario.Id, new RejectUserDto("no cumple los requisitos"), Guid.NewGuid());

        var log = Assert.Single(db.AuditLogs);
        Assert.Contains("no cumple los requisitos", log.Accion);
    }

    [Fact]
    public async Task RejectUserAsync_UsuarioNoExiste_RetornaUsuarioNoEncontrado()
    {
        using var db = CrearDbContext();
        var service = new AuthService(db, CrearConfiguration());

        var resultado = await service.RejectUserAsync(Guid.NewGuid(), new RejectUserDto("motivo"), Guid.NewGuid());

        Assert.False(resultado.Success);
        Assert.Equal(RejectUserError.UserNotFound, resultado.Error);
    }

    [Fact]
    public async Task RejectUserAsync_UsuarioYaRechazado_RetornaEstadoInvalido()
    {
        using var db = CrearDbContext();
        var usuario = CrearUsuarioPendiente(db);
        var service = new AuthService(db, CrearConfiguration());
        await service.RejectUserAsync(usuario.Id, new RejectUserDto("motivo"), Guid.NewGuid());

        var resultado = await service.RejectUserAsync(usuario.Id, new RejectUserDto("otro motivo"), Guid.NewGuid());

        Assert.False(resultado.Success);
        Assert.Equal(RejectUserError.InvalidState, resultado.Error);
    }
}
