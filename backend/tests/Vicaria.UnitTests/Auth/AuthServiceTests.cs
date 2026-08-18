using Microsoft.EntityFrameworkCore;
using Vicaria.Application.Auth;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Auth;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.Auth;

public class AuthServiceTests
{
    private static VicariaDbContext CrearDbContext()
    {
        var options = new DbContextOptionsBuilder<VicariaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new VicariaDbContext(options);
    }

    [Fact]
    public async Task RegisterAsync_ConEmailNuevo_CreaUsuarioYRetornaSuccess()
    {
        using var db = CrearDbContext();
        var service = new AuthService(db);
        var dto = new RegisterDto("Ana", "Perez", "ana@mail.com", "password123");

        var result = await service.RegisterAsync(dto);

        Assert.True(result.Success);
        Assert.NotNull(result.UsuarioId);
        var usuario = await db.Usuarios.SingleAsync();
        Assert.Equal("ana@mail.com", usuario.Email);
    }

    [Fact]
    public async Task RegisterAsync_ConPasswordValido_GuardaHashVerificableConBCrypt()
    {
        using var db = CrearDbContext();
        var service = new AuthService(db);
        var dto = new RegisterDto("Ana", "Perez", "ana@mail.com", "password123");

        await service.RegisterAsync(dto);

        var usuario = await db.Usuarios.SingleAsync();
        Assert.True(BCrypt.Net.BCrypt.Verify("password123", usuario.PasswordHash));
    }

    [Fact]
    public async Task RegisterAsync_ConEmailYaRegistrado_RetornaEmailDuplicado()
    {
        using var db = CrearDbContext();
        db.Usuarios.Add(new Usuario { Id = Guid.NewGuid(), Nombre = "Ana", Apellido = "Perez", Email = "ana@mail.com", PasswordHash = "x", Estado = EstadoUsuario.Pending, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var service = new AuthService(db);
        var dto = new RegisterDto("Otra Ana", "Gomez", "Ana@Mail.com", "password123");

        var result = await service.RegisterAsync(dto);

        Assert.False(result.Success);
        Assert.Equal(1, await db.Usuarios.CountAsync());
    }

    [Fact]
    public async Task RegisterAsync_NormalizaEmailAMinusculas()
    {
        using var db = CrearDbContext();
        var service = new AuthService(db);
        var dto = new RegisterDto("Ana", "Perez", "ANA@MAIL.COM", "password123");

        await service.RegisterAsync(dto);

        var usuario = await db.Usuarios.SingleAsync();
        Assert.Equal("ana@mail.com", usuario.Email);
    }

    [Fact]
    public async Task RegisterAsync_CreaUsuarioConEstadoPending()
    {
        using var db = CrearDbContext();
        var service = new AuthService(db);
        var dto = new RegisterDto("Ana", "Perez", "ana@mail.com", "password123");

        await service.RegisterAsync(dto);

        var usuario = await db.Usuarios.SingleAsync();
        Assert.Equal(EstadoUsuario.Pending, usuario.Estado);
    }
}
