namespace Vicaria.Application.Collaborators;

public class GetCollaboratorByIdResult
{
    public bool IsSuccess => Collaborator is not null;
    public CollaboratorDetailDto? Collaborator { get; }
    public string? ErrorMessage { get; }

    private GetCollaboratorByIdResult(CollaboratorDetailDto? collaborator, string? errorMessage)
    {
        Collaborator = collaborator;
        ErrorMessage = errorMessage;
    }

    public static GetCollaboratorByIdResult Ok(CollaboratorDetailDto collaborator) =>
        new(collaborator, null);

    public static GetCollaboratorByIdResult NotFound(string message = "Colaborador no encontrado.") =>
        new(null, message);
}