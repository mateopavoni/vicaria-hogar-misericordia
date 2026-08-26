using Microsoft.EntityFrameworkCore;
using Vicaria.Application.Auth;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Auth;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.UnitTests.Auth;

public class AuthServiceUserStatusTests
{
    private static VicariaDbContext CrearDbContext()
    {
        var options = new DbContextOptionsBuilder<VicariaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new VicariaDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static User CrearUsuarioConEstado(VicariaDbContext db, UserStatus estado)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Ana",
            LastName = "Perez",
            Email = $"{Guid.NewGuid()}@mail.com",
            PasswordHash = "hash",
            Status = estado,
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    [Fact]
    public async Task DeactivateUserAsync_ConUsuarioActivo_LoDejaInactivo()
    {
        using var db = CrearDbContext();
        var user = CrearUsuarioConEstado(db, UserStatus.Active);
        var service = new AuthService(db);

        var result = await service.DeactivateUserAsync(user.Id, Guid.NewGuid());

        Assert.True(result.Success);
        var usuarioActualizado = await db.Users.FindAsync(user.Id);
        Assert.Equal(UserStatus.Inactive, usuarioActualizado!.Status);
    }

    [Fact]
    public async Task DeactivateUserAsync_ConUsuarioInexistente_DevuelveUserNotFound()
    {
        using var db = CrearDbContext();
        var service = new AuthService(db);

        var result = await service.DeactivateUserAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal(UserStatusError.UserNotFound, result.Error);
    }

    [Fact]
    public async Task ReactivateUserAsync_ConUsuarioInactivo_LoDejaActivoYLimpiaElBloqueo()
    {
        using var db = CrearDbContext();
        var user = CrearUsuarioConEstado(db, UserStatus.Inactive);
        user.FailedLoginAttempts = 5;
        user.LockoutEnd = DateTime.UtcNow.AddMinutes(30);
        db.SaveChanges();
        var service = new AuthService(db);

        var result = await service.ReactivateUserAsync(user.Id, Guid.NewGuid());

        Assert.True(result.Success);
        var usuarioActualizado = await db.Users.FindAsync(user.Id);
        Assert.Equal(UserStatus.Active, usuarioActualizado!.Status);
        Assert.Equal(0, usuarioActualizado.FailedLoginAttempts);
        Assert.Null(usuarioActualizado.LockoutEnd);
    }
}
