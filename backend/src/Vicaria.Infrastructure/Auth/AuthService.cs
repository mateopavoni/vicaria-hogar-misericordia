using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Vicaria.Application.Auth;
using Vicaria.Application.Common;
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

        var exists = await _dbContext.Users.AnyAsync(u => u.Email == email, cancellationToken);
        if (exists)
        {
            return RegisterResult.DuplicateEmail();
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Status = UserStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);

        // avisamos a los referentes que hay una cuenta nueva esperando aprobación
        var hasReferents = await _dbContext.Users.AnyAsync(u => u.Role != null && u.Role.Name == RoleNames.Referente, cancellationToken);
        if (hasReferents)
        {
            _dbContext.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                Description = $"{user.FirstName} {user.LastName} se registró y espera aprobación.",
                EventType = "NuevoUsuarioPendiente",
                LinkUrl = "/usuarios/pendientes",
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                TargetRole = RoleNames.Referente
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return RegisterResult.Ok(user.Id);
    }

    // page size fijo, no lo pide el frontend; si hace falta configurable, exponerlo como query param
    private const int UsersPageSize = 10;

    public async Task<PagedResult<PendingUserDto>> GetPendingUsersAsync(int page = 1, DateTime? dateFrom = null, DateTime? dateTo = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Users
            .Where(u => u.Status == UserStatus.Pending)
            .Where(u => dateFrom == null || u.CreatedAt >= dateFrom)
            .Where(u => dateTo == null || u.CreatedAt <= dateTo)
            .OrderByDescending(u => u.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * UsersPageSize)
            .Take(UsersPageSize)
            .Select(u => new PendingUserDto(u.Id, u.FirstName, u.LastName, u.Email, u.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<PendingUserDto>(items, total, (int)Math.Ceiling(total / (double)UsersPageSize));
    }

    public async Task<PagedResult<ManagedUserDto>> GetActiveUsersAsync(int page = 1, DateTime? dateFrom = null, DateTime? dateTo = null, CancellationToken cancellationToken = default)
    {
        return await GetUsersByStatusAsync(UserStatus.Active, page, dateFrom, dateTo, cancellationToken);
    }

    public async Task<PagedResult<ManagedUserDto>> GetInactiveUsersAsync(int page = 1, DateTime? dateFrom = null, DateTime? dateTo = null, CancellationToken cancellationToken = default)
    {
        return await GetUsersByStatusAsync(UserStatus.Inactive, page, dateFrom, dateTo, cancellationToken);
    }

    private async Task<PagedResult<ManagedUserDto>> GetUsersByStatusAsync(UserStatus status, int page, DateTime? dateFrom, DateTime? dateTo, CancellationToken cancellationToken)
    {
        var query = _dbContext.Users
            .Include(u => u.Role)
            .Where(u => u.Status == status)
            .Where(u => dateFrom == null || u.CreatedAt >= dateFrom)
            .Where(u => dateTo == null || u.CreatedAt <= dateTo)
            .OrderByDescending(u => u.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * UsersPageSize)
            .Take(UsersPageSize)
            .Select(u => new ManagedUserDto(u.Id, u.FirstName, u.LastName, u.Email, u.Role != null ? u.Role.Name : null))
            .ToListAsync(cancellationToken);

        return new PagedResult<ManagedUserDto>(items, total, (int)Math.Ceiling(total / (double)UsersPageSize));
    }

    public async Task<UserStatusResult> UpdateUserRoleAsync(Guid userId, Guid roleId, Guid actorId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FindAsync([userId], cancellationToken);
        if (user is null)
        {
            return UserStatusResult.UserNotFound();
        }

        var roleExists = await _dbContext.Roles.AnyAsync(r => r.Id == roleId, cancellationToken);
        if (!roleExists)
        {
            return UserStatusResult.UserNotFound();
        }

        user.RoleId = roleId;

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = "Rol reasignado",
            AffectedEntity = $"Usuario:{user.Id}",
            Date = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return UserStatusResult.Ok();
    }

    public async Task<ApproveUserResult> ApproveUserAsync(Guid userId, ApproveUserDto dto, Guid actorId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FindAsync([userId], cancellationToken);
        if (user is null)
        {
            return ApproveUserResult.UserNotFound();
        }

        if (user.Status != UserStatus.Pending)
        {
            return ApproveUserResult.InvalidState();
        }

        var roleExists = await _dbContext.Roles.AnyAsync(r => r.Id == dto.RoleId, cancellationToken);
        if (!roleExists)
        {
            return ApproveUserResult.InvalidRole();
        }

        user.Status = UserStatus.Active;
        user.RoleId = dto.RoleId;

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = "AprobarUsuario",
            AffectedEntity = $"Usuario:{user.Id}",
            Date = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApproveUserResult.Ok();
    }

    public async Task<RejectUserResult> RejectUserAsync(Guid userId, RejectUserDto dto, Guid actorId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FindAsync([userId], cancellationToken);
        if (user is null)
        {
            return RejectUserResult.UserNotFound();
        }

        if (user.Status != UserStatus.Pending)
        {
            return RejectUserResult.InvalidState();
        }

        user.Status = UserStatus.Rejected;

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = $"RechazarUsuario: {dto.Reason}",
            AffectedEntity = $"Usuario:{user.Id}",
            Date = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return RejectUserResult.Ok();
    }

    public async Task<LoginResult> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        var user = await _dbContext.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null)
        {
            return LoginResult.InvalidCredentials();
        }

        // cuenta bloqueada por intentos fallidos: no dejamos ni intentar la contraseña
        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
        {
            return LoginResult.AccountLocked(user.LockoutEnd.Value);
        }

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;

            // al 5to intento fallido, bloqueamos la cuenta 30 minutos
            if (user.FailedLoginAttempts >= 5)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(30);

                _dbContext.AuditLogs.Add(new AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Action = "Cuenta bloqueada por 5 intentos fallidos de login",
                    AffectedEntity = $"Usuario:{user.Id}",
                    Date = DateTime.UtcNow
                });

                // avisamos a los referentes que esta cuenta quedó bloqueada (SCRUM-95)
                var hasReferents = await _dbContext.Users.AnyAsync(u => u.Role != null && u.Role.Name == RoleNames.Referente, cancellationToken);
                if (hasReferents)
                {
                    _dbContext.Notifications.Add(new Notification
                    {
                        Id = Guid.NewGuid(),
                        Description = $"La cuenta de {user.FirstName} {user.LastName} quedó bloqueada por 5 intentos fallidos de login.",
                        EventType = "CuentaBloqueada",
                        LinkUrl = "/usuarios",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow,
                        TargetRole = RoleNames.Referente
                    });
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return LoginResult.InvalidCredentials();
        }

        if (user.Status != UserStatus.Active)
        {
            return LoginResult.AccountNotApproved(user.Status.ToString());
        }

        // login correcto: reseteamos el contador de intentos fallidos
        if (user.FailedLoginAttempts > 0 || user.LockoutEnd.HasValue)
        {
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var roleName = user.Role?.Name ?? string.Empty;
        var token = GenerateToken(user, roleName);
        var refreshToken = GenerateRefreshToken();

        // guardamos el refresh token hasheado, igual que la password, nunca en texto plano
        user.RefreshToken = BCrypt.Net.BCrypt.HashPassword(refreshToken);
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(_configuration?.GetValue("Jwt:RefreshTokenExpirationDays", 7) ?? 7);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return LoginResult.Ok(user.Id, user.FirstName, user.LastName, user.Email, roleName, token, refreshToken);
    }

    public async Task<RefreshTokenResult> RefreshTokenAsync(RefreshTokenDto dto, CancellationToken cancellationToken = default)
    {
        // no hay forma de buscar por el token hasheado con un WHERE, así que traemos
        // los usuarios con refresh token activo y comparamos uno por uno con BCrypt
        var users = await _dbContext.Users
            .Include(u => u.Role)
            .Where(u => u.RefreshToken != null)
            .ToListAsync(cancellationToken);

        var user = users.FirstOrDefault(u => BCrypt.Net.BCrypt.Verify(dto.RefreshToken, u.RefreshToken!));
        if (user is null)
        {
            return RefreshTokenResult.InvalidRefreshToken();
        }

        if (user.RefreshTokenExpiry is null || user.RefreshTokenExpiry < DateTime.UtcNow)
        {
            return RefreshTokenResult.RefreshTokenExpired();
        }

        var roleName = user.Role?.Name ?? string.Empty;
        var newToken = GenerateToken(user, roleName);
        var newRefreshToken = GenerateRefreshToken();

        user.RefreshToken = BCrypt.Net.BCrypt.HashPassword(newRefreshToken);
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(_configuration?.GetValue("Jwt:RefreshTokenExpirationDays", 7) ?? 7);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return RefreshTokenResult.Ok(newToken, newRefreshToken);
    }

    public async Task<UserStatusResult> LogoutAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FindAsync([userId], cancellationToken);
        if (user is null)
        {
            return UserStatusResult.UserNotFound();
        }

        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return UserStatusResult.Ok();
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    // arma el JWT con el mismo esquema que valida Program.cs
    private string GenerateToken(User user, string role)
    {
        if (_configuration is null)
        {
            throw new InvalidOperationException("AuthService necesita IConfiguration para emitir el JWT de login.");
        }

        Claim[] claims =
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim()),
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

    public async Task<UserStatusResult> DeactivateUserAsync(Guid userId, Guid actorId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FindAsync([userId], cancellationToken);
        if (user is null)
        {
            return UserStatusResult.UserNotFound();
        }

        if (user.Status == UserStatus.Inactive)
        {
            return UserStatusResult.AlreadyInThatState("El usuario ya se encuentra inactivo.");
        }

        user.Status = UserStatus.Inactive;

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = "Usuario desactivado",
            AffectedEntity = $"Usuario:{user.Id}",
            Date = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return UserStatusResult.Ok();
    }

    public async Task<UserStatusResult> ReactivateUserAsync(Guid userId, Guid actorId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FindAsync([userId], cancellationToken);
        if (user is null)
        {
            return UserStatusResult.UserNotFound();
        }

        if (user.Status == UserStatus.Active)
        {
            return UserStatusResult.AlreadyInThatState("El usuario ya se encuentra activo.");
        }

        user.Status = UserStatus.Active;
        // reactivar también le da al usuario un login limpio, sin arrastrar un bloqueo viejo
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = "Usuario reactivado",
            AffectedEntity = $"Usuario:{user.Id}",
            Date = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return UserStatusResult.Ok();
    }
}
