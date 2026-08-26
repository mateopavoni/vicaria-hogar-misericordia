namespace Vicaria.Application.Auth;

public enum LoginError
{
    InvalidCredentials,
    AccountNotApproved,
    AccountLocked
}

public class LoginResult
{
    public bool Success { get; private init; }
    public string? ErrorMessage { get; private init; }
    public LoginError? Error { get; private init; }
    public string? Status { get; private init; }
    public Guid? UserId { get; private init; }
    public string? FirstName { get; private init; }
    public string? LastName { get; private init; }
    public string? Email { get; private init; }
    public string? Role { get; private init; }
    public string? Token { get; private init; }
    public string? RefreshToken { get; private init; }
    public DateTime? LockoutEnd { get; private init; }

    public static LoginResult Ok(Guid userId, string firstName, string lastName, string email, string role, string token, string refreshToken) =>
        new()
        {
            Success = true,
            UserId = userId,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Role = role,
            Token = token,
            RefreshToken = refreshToken
        };

    public static LoginResult InvalidCredentials() =>
        new()
        {
            Success = false,
            Error = LoginError.InvalidCredentials,
            ErrorMessage = "Credenciales inválidas."
        };

    public static LoginResult AccountNotApproved(string status) =>
        new()
        {
            Success = false,
            Error = LoginError.AccountNotApproved,
            Status = status,
            ErrorMessage = "La cuenta no se encuentra aprobada."
        };

    // el frontend usa "status" para decidir el mensaje de bloqueo, igual que en AccountNotApproved
    public static LoginResult AccountLocked(DateTime lockoutEnd) =>
        new()
        {
            Success = false,
            Error = LoginError.AccountLocked,
            Status = "Bloqueada",
            LockoutEnd = lockoutEnd,
            ErrorMessage = "Cuenta bloqueada temporalmente por exceso de intentos fallidos. Intente más tarde."
        };
}
