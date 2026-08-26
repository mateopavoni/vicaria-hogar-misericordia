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
            ["Jwt:ExpirationMinutes"] = "60"
        };
        return new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
    }

    private static async Task<User> CrearUsuarioConEstado(VicariaDbContext db, UserStatus estado, string password)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Ana",
            LastName = "Perez",
            Email = $"{Guid.NewGuid()}@mail.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Status = estado,
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task LoginAsync_ConCredencialesValidas_DevuelveTokenYUsuario()
    {
        using var db = CrearDbContext();
        var user = await CrearUsuarioConEstado(db, UserStatus.Active, "password123");
        var service = new AuthService(db, CrearConfiguracionJwt());

        var result = await service.LoginAsync(new LoginDto(user.Email, "password123"));

        Assert.True(result.Success);
        Assert.False(string.IsNullOrEmpty(result.Token));
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(user.LastName, result.LastName);
    }

    [Fact]
    public async Task LoginAsync_ConPasswordIncorrecto_DevuelveCredencialesInvalidas()
    {
        using var db = CrearDbContext();
        var user = await CrearUsuarioConEstado(db, UserStatus.Active, "password123");
        var service = new AuthService(db, CrearConfiguracionJwt());

        var result = await service.LoginAsync(new LoginDto(user.Email, "otra-password"));

        Assert.False(result.Success);
        Assert.Equal(LoginError.InvalidCredentials, result.Error);
        Assert.Null(result.Token);
    }

    [Fact]
    public async Task LoginAsync_ConEmailInexistente_DevuelveCredencialesInvalidas()
    {
        using var db = CrearDbContext();
        var service = new AuthService(db, CrearConfiguracionJwt());

        var result = await service.LoginAsync(new LoginDto("no-existe@mail.com", "password123"));

        Assert.False(result.Success);
        Assert.Equal(LoginError.InvalidCredentials, result.Error);
    }

    [Fact]
    public async Task LoginAsync_ConUsuarioPendiente_DevuelveAccountNotApprovedConEstadoPending()
    {
        using var db = CrearDbContext();
        var user = await CrearUsuarioConEstado(db, UserStatus.Pending, "password123");
        var service = new AuthService(db, CrearConfiguracionJwt());

        var result = await service.LoginAsync(new LoginDto(user.Email, "password123"));

        Assert.False(result.Success);
        Assert.Equal(LoginError.AccountNotApproved, result.Error);
        Assert.Equal("Pending", result.Status);
    }

    [Fact]
    public async Task LoginAsync_Con5PasswordsIncorrectos_BloqueaLaCuenta()
    {
        using var db = CrearDbContext();
        var user = await CrearUsuarioConEstado(db, UserStatus.Active, "password123");
        var service = new AuthService(db, CrearConfiguracionJwt());

        for (var i = 0; i < 5; i++)
        {
            await service.LoginAsync(new LoginDto(user.Email, "otra-password"));
        }

        var result = await service.LoginAsync(new LoginDto(user.Email, "password123"));

        Assert.False(result.Success);
        Assert.Equal(LoginError.AccountLocked, result.Error);
        Assert.NotNull(result.LockoutEnd);
    }

    [Fact]
    public async Task LoginAsync_Con5PasswordsIncorrectos_NotificaALosReferentes()
    {
        using var db = CrearDbContext();

        // creamos un referente para que exista alguien a quien notificar
        var rolReferente = new Role { Id = Guid.NewGuid(), Name = RoleNames.Referente };
        db.Roles.Add(rolReferente);
        var referente = await CrearUsuarioConEstado(db, UserStatus.Active, "otraPassword123");
        referente.RoleId = rolReferente.Id;
        await db.SaveChangesAsync();

        var user = await CrearUsuarioConEstado(db, UserStatus.Active, "password123");
        var service = new AuthService(db, CrearConfiguracionJwt());

        for (var i = 0; i < 5; i++)
        {
            await service.LoginAsync(new LoginDto(user.Email, "otra-password"));
        }

        var notificacion = await db.Notifications.SingleOrDefaultAsync(n => n.EventType == "CuentaBloqueada");

        Assert.NotNull(notificacion);
        Assert.Equal(RoleNames.Referente, notificacion!.TargetRole);
    }

    [Fact]
    public async Task LoginAsync_ConLoginCorrecto_ReseteaIntentosFallidosPrevios()
    {
        using var db = CrearDbContext();
        var user = await CrearUsuarioConEstado(db, UserStatus.Active, "password123");
        var service = new AuthService(db, CrearConfiguracionJwt());

        await service.LoginAsync(new LoginDto(user.Email, "otra-password"));
        var result = await service.LoginAsync(new LoginDto(user.Email, "password123"));

        Assert.True(result.Success);
        var usuarioActualizado = await db.Users.FindAsync(user.Id);
        Assert.Equal(0, usuarioActualizado!.FailedLoginAttempts);
    }

    [Fact]
    public async Task LoginAsync_ConCredencialesValidas_DevuelveRefreshToken()
    {
        using var db = CrearDbContext();
        var user = await CrearUsuarioConEstado(db, UserStatus.Active, "password123");
        var service = new AuthService(db, CrearConfiguracionJwt());

        var result = await service.LoginAsync(new LoginDto(user.Email, "password123"));

        Assert.False(string.IsNullOrEmpty(result.RefreshToken));
    }

    [Fact]
    public async Task RefreshTokenAsync_ConTokenValido_DevuelveTokensNuevos()
    {
        using var db = CrearDbContext();
        var user = await CrearUsuarioConEstado(db, UserStatus.Active, "password123");
        var service = new AuthService(db, CrearConfiguracionJwt());
        var login = await service.LoginAsync(new LoginDto(user.Email, "password123"));

        var result = await service.RefreshTokenAsync(new RefreshTokenDto(login.RefreshToken!));

        Assert.True(result.Success);
        Assert.False(string.IsNullOrEmpty(result.Token));
        Assert.NotEqual(login.RefreshToken, result.RefreshToken);
    }

    [Fact]
    public async Task RefreshTokenAsync_ConTokenInexistente_DevuelveInvalido()
    {
        using var db = CrearDbContext();
        var service = new AuthService(db, CrearConfiguracionJwt());

        var result = await service.RefreshTokenAsync(new RefreshTokenDto("token-que-no-existe"));

        Assert.False(result.Success);
        Assert.Equal(RefreshTokenError.InvalidRefreshToken, result.Error);
    }

    [Fact]
    public async Task LogoutAsync_BorraElRefreshTokenDelUsuario()
    {
        using var db = CrearDbContext();
        var user = await CrearUsuarioConEstado(db, UserStatus.Active, "password123");
        var service = new AuthService(db, CrearConfiguracionJwt());
        await service.LoginAsync(new LoginDto(user.Email, "password123"));

        await service.LogoutAsync(user.Id);

        var usuarioActualizado = await db.Users.FindAsync(user.Id);
        Assert.Null(usuarioActualizado!.RefreshToken);
    }
}
