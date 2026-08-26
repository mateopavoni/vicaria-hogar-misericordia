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

    private static Usuario CrearUsuarioConEstado(VicariaDbContext db, EstadoUsuario estado)
    {
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Nombre = "Ana",
            Apellido = "Perez",
            Email = $"{Guid.NewGuid()}@mail.com",
            PasswordHash = "hash",
            Estado = estado,
            CreatedAt = DateTime.UtcNow
        };
        db.Usuarios.Add(usuario);
        db.SaveChanges();
        return usuario;
    }

    [Fact]
    public async Task DeactivateUserAsync_ConUsuarioActivo_LoDejaInactivo()
    {
        using var db = CrearDbContext();
        var usuario = CrearUsuarioConEstado(db, EstadoUsuario.Active);
        var service = new AuthService(db);

        var result = await service.DeactivateUserAsync(usuario.Id, Guid.NewGuid());

        Assert.True(result.Success);
        var usuarioActualizado = await db.Usuarios.FindAsync(usuario.Id);
        Assert.Equal(EstadoUsuario.Inactive, usuarioActualizado!.Estado);
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
        var usuario = CrearUsuarioConEstado(db, EstadoUsuario.Inactive);
        usuario.FailedLoginAttempts = 5;
        usuario.LockoutEnd = DateTime.UtcNow.AddMinutes(30);
        db.SaveChanges();
        var service = new AuthService(db);

        var result = await service.ReactivateUserAsync(usuario.Id, Guid.NewGuid());

        Assert.True(result.Success);
        var usuarioActualizado = await db.Usuarios.FindAsync(usuario.Id);
        Assert.Equal(EstadoUsuario.Active, usuarioActualizado!.Estado);
        Assert.Equal(0, usuarioActualizado.FailedLoginAttempts);
        Assert.Null(usuarioActualizado.LockoutEnd);
    }
}
