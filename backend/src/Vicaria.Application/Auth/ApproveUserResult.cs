namespace Vicaria.Application.Auth;

public enum ApproveUserError
{
    UsuarioNoEncontrado,
    EstadoInvalido,
    RolInvalido
}

// resultado explícito en vez de excepción: son casos de negocio esperados
public class ApproveUserResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public ApproveUserError? Error { get; init; }

    public static ApproveUserResult Ok() => new() { Success = true };

    public static ApproveUserResult UsuarioNoEncontrado() =>
        new() { Success = false, Error = ApproveUserError.UsuarioNoEncontrado, ErrorMessage = "El usuario no existe." };

    public static ApproveUserResult EstadoInvalido() =>
        new() { Success = false, Error = ApproveUserError.EstadoInvalido, ErrorMessage = "El usuario no está pendiente de aprobación." };

    public static ApproveUserResult RolInvalido() =>
        new() { Success = false, Error = ApproveUserError.RolInvalido, ErrorMessage = "El rol indicado no existe." };
}
