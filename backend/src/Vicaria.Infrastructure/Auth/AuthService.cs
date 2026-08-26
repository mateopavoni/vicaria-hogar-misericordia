using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
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
    private readonly IConfiguration _configuration;

    public AuthService(VicariaDbContext dbContext, IConfiguration configuration)
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

        var referentes = await _dbContext.Usuarios
            .Where(u => u.Rol != null && u.Rol.Nombre == RolNombres.Referente)
            .ToListAsync(cancellationToken);

        if (referentes.Count > 0)
        {
            _dbContext.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                Description = $"Nuevo usuario registrado: {email} ({dto.Nombre} {dto.Apellido})",
                EventType = "NewUserPendingApproval",
                LinkUrl = $"/usuarios/{usuario.Id}",
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                TargetRole = RolNombres.Referente
            });

            _dbContext.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                UsuarioId = usuario.Id,
                Accion = "CrearNotificacion",
                EntidadAfectada = $"Notificacion:NuevoPendiente:{usuario.Id}",
                Fecha = DateTime.UtcNow
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

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
        if (usuario is null) return ApproveUserResult.UserNotFound();
        if (usuario.Estado != EstadoUsuario.Pending) return ApproveUserResult.InvalidState();

        var roleExists = await _dbContext.Roles.AnyAsync(r => r.Id == dto.RolId, cancellationToken);
        if (!roleExists) return ApproveUserResult.InvalidRole();

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
        if (usuario is null) return RejectUserResult.UserNotFound();
        if (usuario.Estado != EstadoUsuario.Pending) return RejectUserResult.InvalidState();

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
            return LoginResult.UserNotFound();
        }

        if (usuario.Estado != EstadoUsuario.Active)
        {
            return LoginResult.InvalidState();
        }

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash))
        {
            return LoginResult.InvalidCredentials();
        }

        var accessToken = GenerateAccessToken(usuario);
        var refreshToken = GenerateRefreshToken();

        usuario.RefreshToken = BCrypt.Net.BCrypt.HashPassword(refreshToken);
        usuario.RefreshTokenExpiry = DateTime.UtcNow.AddDays(_configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7));

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            Accion = "Login",
            EntidadAfectada = $"Usuario:{usuario.Id}",
            Fecha = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return LoginResult.Ok(accessToken, refreshToken);
    }

    public async Task<RefreshTokenResult> RefreshTokenAsync(RefreshTokenDto dto, CancellationToken cancellationToken = default)
    {
        var usuarios = await _dbContext.Usuarios
            .Include(u => u.Rol)
            .Where(u => u.RefreshToken != null && u.RefreshTokenExpiry != null)
            .ToListAsync(cancellationToken);

        var usuario = usuarios.FirstOrDefault(u =>
            BCrypt.Net.BCrypt.Verify(dto.RefreshToken, u.RefreshToken!));

        if (usuario is null)
        {
            return RefreshTokenResult.InvalidRefreshToken();
        }

        if (usuario.RefreshTokenExpiry! < DateTime.UtcNow)
        {
            return RefreshTokenResult.RefreshTokenExpired();
        }

        var newAccessToken = GenerateAccessToken(usuario);
        var newRefreshToken = GenerateRefreshToken();

        usuario.RefreshToken = BCrypt.Net.BCrypt.HashPassword(newRefreshToken);
        usuario.RefreshTokenExpiry = DateTime.UtcNow.AddDays(_configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7));

        await _dbContext.SaveChangesAsync(cancellationToken);
        return RefreshTokenResult.Ok(newAccessToken, newRefreshToken);
    }

    public async Task<LogoutResult> LogoutAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var usuario = await _dbContext.Usuarios.FindAsync([userId], cancellationToken);
        if (usuario is null)
        {
            return LogoutResult.Error("El usuario no existe.");
        }

        usuario.RefreshToken = null;
        usuario.RefreshTokenExpiry = null;

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UsuarioId = userId,
            Accion = "Logout",
            EntidadAfectada = $"Usuario:{userId}",
            Fecha = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return LogoutResult.Ok();
    }

    private string GenerateAccessToken(Usuario usuario)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expirationMinutes = _configuration.GetValue<int>("Jwt:ExpirationMinutes", 1440);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, $"{usuario.Nombre} {usuario.Apellido}"),
            new(ClaimTypes.Email, usuario.Email)
        };

        if (usuario.Rol is not null)
        {
            claims.Add(new Claim(ClaimTypes.Role, usuario.Rol.Nombre));
        }

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
