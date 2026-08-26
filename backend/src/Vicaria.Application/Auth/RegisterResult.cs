namespace Vicaria.Application.Auth;

public class RegisterResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? UsuarioId { get; init; }

    public static RegisterResult Ok(Guid usuarioId) =>
        new() { Success = true, UsuarioId = usuarioId };

    public static RegisterResult DuplicateEmail() =>
        new() { Success = false, ErrorMessage = "Ya existe un usuario registrado con ese email." };
}
