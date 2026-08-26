namespace Vicaria.Application.Auth;

public interface IAuthService
{
    Task<RegisterResult> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PendingUserDto>> GetPendingUsersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ManagedUserDto>> GetActiveUsersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ManagedUserDto>> GetInactiveUsersAsync(CancellationToken cancellationToken = default);
    Task<UserStatusResult> UpdateUserRoleAsync(Guid userId, Guid roleId, Guid actorId, CancellationToken cancellationToken = default);
    Task<ApproveUserResult> ApproveUserAsync(Guid userId, ApproveUserDto dto, Guid actorId, CancellationToken cancellationToken = default);
    Task<RejectUserResult> RejectUserAsync(Guid userId, RejectUserDto dto, Guid actorId, CancellationToken cancellationToken = default);
    Task<LoginResult> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);
    Task<UserStatusResult> DeactivateUserAsync(Guid userId, Guid actorId, CancellationToken cancellationToken = default);
    Task<UserStatusResult> ReactivateUserAsync(Guid userId, Guid actorId, CancellationToken cancellationToken = default);
    Task<RefreshTokenResult> RefreshTokenAsync(RefreshTokenDto dto, CancellationToken cancellationToken = default);
    Task<UserStatusResult> LogoutAsync(Guid userId, CancellationToken cancellationToken = default);
}
