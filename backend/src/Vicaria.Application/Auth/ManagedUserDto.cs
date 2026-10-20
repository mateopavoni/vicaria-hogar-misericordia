namespace Vicaria.Application.Auth;

public record ManagedUserDto(Guid Id, string FirstName, string LastName, string Email, string? Role);
