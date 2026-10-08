namespace Vicaria.Application.Collaborators;

public enum CreateCollaboratorError
{
    DuplicateDni
}

public class CreateCollaboratorResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public CreateCollaboratorError? Error { get; init; }
    public Guid CollaboratorId { get; init; }

    public static CreateCollaboratorResult Ok(Guid collaboratorId) => new()
    {
        Success = true,
        CollaboratorId = collaboratorId
    };

    public static CreateCollaboratorResult DuplicateDni() =>
        new()
        {
            Success = false,
            Error = CreateCollaboratorError.DuplicateDni,
            ErrorMessage = "Ya existe un colaborador con ese DNI."
        };
}
