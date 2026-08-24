namespace Vicaria.Application.Auth;

public enum LoginError
{
    InvalidCredentials,
    AccountNotApproved
}

public class LoginResult
{
    public bool Success { get; private init; }
    public string? ErrorMessage { get; private init; }
    public LoginError? Error { get; private init; }
    public Guid? UsuarioId { get; private init; }
    public string? Nombre { get; private init; }
    public string? Email { get; private init; }
    public string? Rol { get; private init; }

    public static LoginResult Ok(Guid usuarioId, string nombre, string email, string rol) =>
        new()
        {
            Success = true,
            UsuarioId = usuarioId,
            Nombre = nombre,
            Email = email,
            Rol = rol
        };

    public static LoginResult InvalidCredentials() =>
        new()
        {
            Success = false,
            Error = LoginError.InvalidCredentials,
            ErrorMessage = "Credenciales inválidas."
        };

    public static LoginResult AccountNotApproved() =>
        new()
        {
            Success = false,
            Error = LoginError.AccountNotApproved,
            ErrorMessage = "La cuenta no se encuentra aprobada."
        };
}