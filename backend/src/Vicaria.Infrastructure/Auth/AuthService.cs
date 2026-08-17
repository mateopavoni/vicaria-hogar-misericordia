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
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Usuarios.Add(usuario);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return RegisterResult.Ok(usuario.Id);
    }
}
