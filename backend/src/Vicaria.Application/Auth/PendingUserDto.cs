namespace Vicaria.Application.Auth;

public record PendingUserDto(Guid Id, string Nombre, string Apellido, string Email, DateTime FechaSolicitud);
