using Vicaria.Domain.Entities;

namespace Vicaria.Application.Collaborators;

// alta de colaborador (SCRUM-199): solo FirstName es obligatorio; Dni, Email y WorkArea
// son opcionales. RegisteredByUserId y RegisteredAt los toma el servicio del JWT, no vienen del cliente.
public record CreateCollaboratorDto(
    string FirstName,
    string? LastName,
    string? Dni,
    string? Phone,
    string? Email,
    CollaboratorType Type,
    string? WorkArea);
