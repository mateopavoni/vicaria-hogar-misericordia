namespace Vicaria.Application.Auth;

public enum RejectUserError
{
    UsuarioNoEncontrado,
    EstadoInvalido
}

public class RejectUserResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public RejectUserError? Error { get; init; }

    public static RejectUserResult Ok() => new() { Success = true };

    public static RejectUserResult UsuarioNoEncontrado() =>
        new() { Success = false, Error = RejectUserError.UsuarioNoEncontrado, ErrorMessage = "El usuario no existe." };

    public static RejectUserResult EstadoInvalido() =>
        new() { Success = false, Error = RejectUserError.EstadoInvalido, ErrorMessage = "El usuario no está pendiente de aprobación." };
}
