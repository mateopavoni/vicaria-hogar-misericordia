using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Vicaria.Application.Auth;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Auth;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.Auth;

public class AuthServiceLoginTests
{
    private static VicariaDbContext CrearDbContext()
    {
        var options = new DbContextOptionsBuilder<VicariaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new VicariaDbContext(options);
    }

    private static IConfiguration CrearConfiguracionJwt()
    {
        var valores = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "clave-de-test-no-usar-en-produccion-vicaria-2026",
            ["Jwt:Issuer"] = "VicariaApi",
            ["Jwt:Audience"] = "VicariaApi",
            ["Jwt:ExpirationMinutes"] = "1440",
            ["Jwt:RefreshTokenExpirationDays"] = "7"
        };
        return new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
    }

    private static async Task<Usuario> CrearUsuarioConEstado(VicariaDbContext db, EstadoUsuario estado, string password)
    {
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Nombre = "Ana",
            Apellido = "Perez",
            Email = $"{Guid.NewGuid()}@mail.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Estado = estado,
            CreatedAt = DateTime.UtcNow
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        return usuario;
    }

    [Fact]
    public async Task LoginAsync_ConCredencialesValidas_DevuelveTokens()
    {
        using var db = CrearDbContext();
        var usuario = await CrearUsuarioConEstado(db, EstadoUsuario.Active, "password123");
        var service = new AuthService(db, CrearConfiguracionJwt());

        var result = await service.LoginAsync(new LoginDto(usuario.Email, "password123"));

        Assert.True(result.Success);
        Assert.False(string.IsNullOrEmpty(result.AccessToken));
        Assert.False(string.IsNullOrEmpty(result.RefreshToken));
    }

    [Fact]
    public async Task LoginAsync_ConPasswordIncorrecto_DevuelveCredencialesInvalidas()
    {
        using var db = CrearDbContext();
        var usuario = await CrearUsuarioConEstado(db, EstadoUsuario.Active, "password123");
        var service = new AuthService(db, CrearConfiguracionJwt());

        var result = await service.LoginAsync(new LoginDto(usuario.Email, "otra-password"));

        Assert.False(result.Success);
        Assert.Equal(LoginError.InvalidCredentials, result.Error);
        Assert.Null(result.AccessToken);
        Assert.Null(result.RefreshToken);
    }

    [Fact]
    public async Task LoginAsync_ConEmailInexistente_DevuelveUsuarioNoEncontrado()
    {
        using var db = CrearDbContext();
        var service = new AuthService(db, CrearConfiguracionJwt());

        var result = await service.LoginAsync(new LoginDto("no-existe@mail.com", "password123"));

        Assert.False(result.Success);
        Assert.Equal(LoginError.UserNotFound, result.Error);
    }

    [Fact]
    public async Task LoginAsync_ConUsuarioPendiente_DevuelveEstadoInvalido()
    {
        using var db = CrearDbContext();
        var usuario = await CrearUsuarioConEstado(db, EstadoUsuario.Pending, "password123");
        var service = new AuthService(db, CrearConfiguracionJwt());

        var result = await service.LoginAsync(new LoginDto(usuario.Email, "password123"));

        Assert.False(result.Success);
        Assert.Equal(LoginError.InvalidState, result.Error);
    }

    [Fact]
    public async Task LoginAsync_RegistraAuditLog()
    {
        using var db = CrearDbContext();
        var usuario = await CrearUsuarioConEstado(db, EstadoUsuario.Active, "password123");
        var service = new AuthService(db, CrearConfiguracionJwt());

        await service.LoginAsync(new LoginDto(usuario.Email, "password123"));

        var log = Assert.Single(db.AuditLogs);
        Assert.Equal(usuario.Id, log.UsuarioId);
        Assert.Equal("Login", log.Accion);
    }
}
