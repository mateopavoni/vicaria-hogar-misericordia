namespace Vicaria.Application.Auth;

public record ManagedUserDto(Guid Id, string Nombre, string Apellido, string Email, string? Rol);
