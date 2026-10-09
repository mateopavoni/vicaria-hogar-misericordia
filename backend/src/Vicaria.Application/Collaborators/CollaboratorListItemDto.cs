using Vicaria.Domain.Entities;

namespace Vicaria.Application.Collaborators;

// ítem del listado completo de colaboradores (pantalla de gestión): a diferencia del resultado
// de búsqueda trae nombre y apellido por separado, DNI y quién lo registró
public record CollaboratorListItemDto(
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
    string? RegisteredByName);
