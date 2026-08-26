namespace Vicaria.Application.Auth;

public class RegisterResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? UserId { get; init; }

    public static RegisterResult Ok(Guid userId) =>
        new() { Success = true, UserId = userId };

    public static RegisterResult DuplicateEmail() =>
        new() { Success = false, ErrorMessage = "Ya existe un usuario registrado con ese email." };
}
