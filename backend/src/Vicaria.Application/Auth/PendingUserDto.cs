namespace Vicaria.Application.Auth;

public record PendingUserDto(Guid Id, string FirstName, string LastName, string Email, DateTime RequestDate);
