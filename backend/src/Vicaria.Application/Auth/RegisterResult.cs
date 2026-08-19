namespace Vicaria.Application.Auth;

// resultado explícito en vez de excepción: el email duplicado es un caso de negocio esperado
public class RegisterResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? UsuarioId { get; init; }

    public static RegisterResult Ok(Guid usuarioId) =>
        new() { Success = true, UsuarioId = usuarioId };

    public static RegisterResult EmailDuplicado() =>
        new() { Success = false, ErrorMessage = "Ya existe un usuario registrado con ese email." };
}
