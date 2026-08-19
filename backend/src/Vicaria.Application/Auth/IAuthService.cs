namespace Vicaria.Application.Auth;

public interface IAuthService
{
    Task<RegisterResult> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default);
}
