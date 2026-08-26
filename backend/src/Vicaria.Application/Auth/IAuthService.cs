namespace Vicaria.Application.Auth;

public interface IAuthService
{
    Task<RegisterResult> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PendingUserDto>> GetPendingUsersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ManagedUserDto>> GetActiveUsersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ManagedUserDto>> GetInactiveUsersAsync(CancellationToken cancellationToken = default);
    Task<UserStatusResult> UpdateUserRoleAsync(Guid usuarioId, Guid rolId, Guid actorId, CancellationToken cancellationToken = default);
    Task<ApproveUserResult> ApproveUserAsync(Guid usuarioId, ApproveUserDto dto, Guid actorId, CancellationToken cancellationToken = default);
    Task<RejectUserResult> RejectUserAsync(Guid usuarioId, RejectUserDto dto, Guid actorId, CancellationToken cancellationToken = default);
    Task<LoginResult> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);
    Task<UserStatusResult> DeactivateUserAsync(Guid usuarioId, Guid actorId, CancellationToken cancellationToken = default);
    Task<UserStatusResult> ReactivateUserAsync(Guid usuarioId, Guid actorId, CancellationToken cancellationToken = default);
    Task<RefreshTokenResult> RefreshTokenAsync(RefreshTokenDto dto, CancellationToken cancellationToken = default);
    Task<UserStatusResult> LogoutAsync(Guid usuarioId, CancellationToken cancellationToken = default);
}
