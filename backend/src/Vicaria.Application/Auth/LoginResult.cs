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
    public string? Estado { get; private init; }
    public Guid? UsuarioId { get; private init; }
    public string? Nombre { get; private init; }
    public string? Apellido { get; private init; }
    public string? Email { get; private init; }
    public string? Rol { get; private init; }
    public string? Token { get; private init; }

    public static LoginResult Ok(Guid usuarioId, string nombre, string apellido, string email, string rol, string token) =>
        new()
        {
            Success = true,
            UsuarioId = usuarioId,
            Nombre = nombre,
            Apellido = apellido,
            Email = email,
            Rol = rol,
            Token = token
        };

    public static LoginResult InvalidCredentials() =>
        new()
        {
            Success = false,
            Error = LoginError.InvalidCredentials,
            ErrorMessage = "Credenciales inválidas."
        };

    public static LoginResult AccountNotApproved(string estado) =>
        new()
        {
            Success = false,
            Error = LoginError.AccountNotApproved,
            Estado = estado,
            ErrorMessage = "La cuenta no se encuentra aprobada."
        };
}
