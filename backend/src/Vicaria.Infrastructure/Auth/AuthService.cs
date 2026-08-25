using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Vicaria.Application.Auth;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.Infrastructure.Auth;

public class AuthService : IAuthService
{
    private readonly VicariaDbContext _dbContext;
    private readonly IConfiguration? _configuration;

    public AuthService(VicariaDbContext dbContext, IConfiguration? configuration = null)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    public async Task<RegisterResult> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        var exists = await _dbContext.Usuarios.AnyAsync(u => u.Email == email, cancellationToken);
        if (exists)
        {
            return RegisterResult.DuplicateEmail();
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
            return ApproveUserResult.UserNotFound();
        }

        if (usuario.Estado != EstadoUsuario.Pending)
        {
            return ApproveUserResult.InvalidState();
        }

        var roleExists = await _dbContext.Roles.AnyAsync(r => r.Id == dto.RolId, cancellationToken);
        if (!roleExists)
        {
            return ApproveUserResult.InvalidRole();
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
            return RejectUserResult.UserNotFound();
        }

        if (usuario.Estado != EstadoUsuario.Pending)
        {
            return RejectUserResult.InvalidState();
        }

        usuario.Estado = EstadoUsuario.Rejected;

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

    public async Task<LoginResult> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        var usuario = await _dbContext.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (usuario is null)
        {
            return LoginResult.InvalidCredentials();
        }

        if (usuario.LockoutEnd.HasValue && usuario.LockoutEnd.Value > DateTime.UtcNow)
        {
            return LoginResult.AccountLocked(usuario.LockoutEnd.Value);
        }

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash))
        {
            usuario.FailedLoginAttempts++;

            if (usuario.FailedLoginAttempts >= 5)
            {
                usuario.LockoutEnd = DateTime.UtcNow.AddMinutes(30);

                _dbContext.AuditLogs.Add(new AuditLog
                {
                    Id = Guid.NewGuid(),
                    UsuarioId = usuario.Id,
                    Accion = "Cuenta bloqueada: 5 intentos fallidos",
                    EntidadAfectada = $"Usuario:{usuario.Id}",
                    Fecha = DateTime.UtcNow
                });
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return LoginResult.InvalidCredentials();
        }

        if (usuario.Estado != EstadoUsuario.Active)
        {
            return LoginResult.AccountNotApproved(usuario.Estado.ToString());
        }

        if (usuario.FailedLoginAttempts > 0 || usuario.LockoutEnd.HasValue)
        {
            usuario.FailedLoginAttempts = 0;
            usuario.LockoutEnd = null;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var roleName = usuario.Rol?.Nombre ?? string.Empty;
        var token = GenerateToken(usuario, roleName);

        return LoginResult.Ok(usuario.Id, usuario.Nombre, usuario.Apellido, usuario.Email, roleName, token);
    }
    public async Task<(bool Success, string ErrorMessage)> DeactivateUserAsync(Guid id, Guid actorId, CancellationToken cancellationToken)
{
    var user = await _dbContext.Usuarios.FindAsync(new object[] { id }, cancellationToken);
    if (user == null)
    {
        return (false, "Usuario no encontrado.");
    }

    if (user.Estado == EstadoUsuario.Inactive)
    {
        return (false, "El usuario ya se encuentra inactivo.");
    }

    user.Estado = EstadoUsuario.Inactive;
    
    await _dbContext.SaveChangesAsync(cancellationToken);
    return (true, string.Empty);
}

public async Task<(bool Success, string ErrorMessage)> ReactivateUserAsync(Guid id, Guid actorId, CancellationToken cancellationToken)
{
    var user = await _dbContext.Usuarios.FindAsync(new object[] { id }, cancellationToken);
    if (user == null)
    {
        return (false, "Usuario no encontrado.");
    }

    if (user.Estado == EstadoUsuario.Active)
    {
        return (false, "El usuario ya se encuentra activo.");
    }

    user.Estado = EstadoUsuario.Active;

    user.FailedLoginAttempts = 0;
    user.LockoutEnd = null;

    await _dbContext.SaveChangesAsync(cancellationToken);
    return (true, string.Empty);
}

    // arma el JWT con el mismo esquema que valida Program.cs
    private string GenerateToken(Usuario user, string role)
    {
        if (_configuration is null)
        {
            throw new InvalidOperationException("AuthService necesita IConfiguration para emitir el JWT de login.");
        }

        Claim[] claims =
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, $"{user.Nombre} {user.Apellido}".Trim()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, role)
        ];

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? string.Empty));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expirationMinutes = _configuration.GetValue("Jwt:ExpirationMinutes", 60);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
