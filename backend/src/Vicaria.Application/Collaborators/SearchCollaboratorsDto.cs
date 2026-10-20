using Vicaria.Domain.Entities;

namespace Vicaria.Application.Collaborators;

// filtro de búsqueda de colaboradores (SCRUM-204): q es texto libre sobre nombre,
// apellido o área de trabajo; type es opcional (Volunteer/Employee)
public record SearchCollaboratorsDto(
    string? Q = null,
    CollaboratorType? Type = null);
