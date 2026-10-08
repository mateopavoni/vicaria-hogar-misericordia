using Vicaria.Domain.Entities;

namespace Vicaria.Application.Collaborators;

public interface ICollaboratorService
{
    Task<CreateCollaboratorResult> CreateAsync(
        CreateCollaboratorDto dto,
        Guid actorId,
        CancellationToken cancellationToken);

        Task<UpdateCollaboratorResult> UpdateAsync(
        Guid id,
        UpdateCollaboratorDto dto,
        Guid actorId,
        CancellationToken cancellationToken);

    // búsqueda por nombre, apellido o área con filtro opcional por tipo (SCRUM-204)
    Task<List<CollaboratorSearchResultDto>> SearchAsync(
        string? query,
        CollaboratorType? typeFilter = null,
        CancellationToken cancellationToken = default);

        Task<GetCollaboratorByIdResult> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
