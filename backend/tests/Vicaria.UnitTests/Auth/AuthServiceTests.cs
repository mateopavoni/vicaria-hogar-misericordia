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
        Assert.NotNull(result.UserId);
        var user = await db.Users.SingleAsync();
        Assert.Equal("ana@mail.com", user.Email);
    }

    [Fact]
    public async Task RegisterAsync_ConPasswordValido_GuardaHashVerificableConBCrypt()
    {
        using var db = CrearDbContext();
        var service = new AuthService(db);
        var dto = new RegisterDto("Ana", "Perez", "ana@mail.com", "password123");

        await service.RegisterAsync(dto);

        var user = await db.Users.SingleAsync();
        Assert.True(BCrypt.Net.BCrypt.Verify("password123", user.PasswordHash));
    }

    [Fact]
    public async Task RegisterAsync_ConEmailYaRegistrado_RetornaEmailDuplicado()
    {
        using var db = CrearDbContext();
        db.Users.Add(new User { Id = Guid.NewGuid(), FirstName = "Ana", LastName = "Perez", Email = "ana@mail.com", PasswordHash = "x", Status = UserStatus.Pending, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var service = new AuthService(db);
        var dto = new RegisterDto("Otra Ana", "Gomez", "Ana@Mail.com", "password123");

        var result = await service.RegisterAsync(dto);

        Assert.False(result.Success);
        Assert.Equal(1, await db.Users.CountAsync());
    }

    [Fact]
    public async Task RegisterAsync_NormalizaEmailAMinusculas()
    {
        using var db = CrearDbContext();
        var service = new AuthService(db);
        var dto = new RegisterDto("Ana", "Perez", "ANA@MAIL.COM", "password123");

        await service.RegisterAsync(dto);

        var user = await db.Users.SingleAsync();
        Assert.Equal("ana@mail.com", user.Email);
    }

    [Fact]
    public async Task RegisterAsync_CreaUsuarioConEstadoPending()
    {
        using var db = CrearDbContext();
        var service = new AuthService(db);
        var dto = new RegisterDto("Ana", "Perez", "ana@mail.com", "password123");

        await service.RegisterAsync(dto);

        var user = await db.Users.SingleAsync();
        Assert.Equal(UserStatus.Pending, user.Status);
    }
}
