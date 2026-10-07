namespace Vicaria.Application.Collaborators;

public interface ICollaboratorService
{
    Task<CreateCollaboratorResult> CreateAsync(
        CreateCollaboratorDto dto,
        Guid actorId,
        CancellationToken cancellationToken);
}
