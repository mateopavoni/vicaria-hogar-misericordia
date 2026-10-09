using Vicaria.Domain.Entities;

namespace Vicaria.Application.Collaborators;

// resultado de búsqueda de colaboradores (SCRUM-204): la historia pide nombre completo,
// teléfono, email y tipo; Id habilita abrir la ficha y WorkArea es el campo con el que se busca
public record CollaboratorSearchResultDto(
    Guid Id,
    string FullName,
    string? Phone,
    string? Email,
    CollaboratorType Type,
    string? WorkArea);
