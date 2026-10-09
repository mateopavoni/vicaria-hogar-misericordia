using Vicaria.Domain.Entities;

namespace Vicaria.Application.Collaborators;

public record CollaboratorDetailDto(
    Guid Id,
    string FirstName,
    string? LastName,
    string? Dni,
    string? Phone,
    string? Email,
    CollaboratorType Type,
    string? WorkArea,
    bool IsActive,
    DateTime RegisteredAt,
    Guid RegisteredByUserId,
    string RegisteredByUserName);