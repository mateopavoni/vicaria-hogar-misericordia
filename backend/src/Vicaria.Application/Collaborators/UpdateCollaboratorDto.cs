using Vicaria.Domain.Entities;

namespace Vicaria.Application.Collaborators;

public record UpdateCollaboratorDto(
    string FirstName,
    string? LastName,
    string? Dni,
    string? Phone,
    string? Email,
    CollaboratorType Type,
    string? WorkArea,
    bool IsActive);