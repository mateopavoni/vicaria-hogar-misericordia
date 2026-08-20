using Microsoft.EntityFrameworkCore;
using Vicaria.Application.Auth;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.Infrastructure.Auth;

public class AuthService : IAuthService
{
    private readonly VicariaDbContext _dbContext;

    public AuthService(VicariaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RegisterResult> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        var existe = await _dbContext.Usuarios.AnyAsync(u => u.Email == email, cancellationToken);
        if (existe)
        {
            return RegisterResult.EmailDuplicado();
        }

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Nombre = dto.Nombre.Trim(),
            Apellido = dto.Apellido.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Estado = EstadoUsuario.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Usuarios.Add(usuario);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return RegisterResult.Ok(usuario.Id);
    }

    public async Task<IReadOnlyList<PendingUserDto>> GetPendingUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Usuarios
            .Where(u => u.Estado == EstadoUsuario.Pending)
            .Select(u => new PendingUserDto(u.Id, u.Email, u.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<ApproveUserResult> ApproveUserAsync(Guid usuarioId, ApproveUserDto dto, Guid actorId, CancellationToken cancellationToken = default)
    {
        var usuario = await _dbContext.Usuarios.FindAsync([usuarioId], cancellationToken);
        if (usuario is null)
        {
            return ApproveUserResult.UsuarioNoEncontrado();
        }

        if (usuario.Estado != EstadoUsuario.Pending)
        {
            return ApproveUserResult.EstadoInvalido();
        }

        var rolExiste = await _dbContext.Roles.AnyAsync(r => r.Id == dto.RolId, cancellationToken);
        if (!rolExiste)
        {
            return ApproveUserResult.RolInvalido();
        }

        usuario.Estado = EstadoUsuario.Active;
        usuario.RolId = dto.RolId;

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UsuarioId = actorId,
            Accion = "AprobarUsuario",
            EntidadAfectada = $"Usuario:{usuario.Id}",
            Fecha = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApproveUserResult.Ok();
    }

    public async Task<RejectUserResult> RejectUserAsync(Guid usuarioId, RejectUserDto dto, Guid actorId, CancellationToken cancellationToken = default)
    {
        var usuario = await _dbContext.Usuarios.FindAsync([usuarioId], cancellationToken);
        if (usuario is null)
        {
            return RejectUserResult.UsuarioNoEncontrado();
        }

        if (usuario.Estado != EstadoUsuario.Pending)
        {
            return RejectUserResult.EstadoInvalido();
        }

        usuario.Estado = EstadoUsuario.Rejected;

        // el motivo no tiene columna propia en el pedido (solo UsuarioId/Accion/EntidadAfectada/Fecha),
        // se guarda dentro de Accion para no perder el contexto del rechazo
        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UsuarioId = actorId,
            Accion = $"RechazarUsuario: {dto.Motivo}",
            EntidadAfectada = $"Usuario:{usuario.Id}",
            Fecha = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return RejectUserResult.Ok();
    }
}
