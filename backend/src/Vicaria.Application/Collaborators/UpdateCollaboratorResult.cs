namespace Vicaria.Application.Collaborators;

public enum UpdateCollaboratorError
{
    NotFound,
    DuplicateDni
}

public class UpdateCollaboratorResult
{
    public bool IsSuccess => Error is null;
    public UpdateCollaboratorError? Error { get; }
    public string? ErrorMessage { get; }

    private UpdateCollaboratorResult(UpdateCollaboratorError? error, string? errorMessage)
    {
        Error = error;
        ErrorMessage = errorMessage;
    }

    public static UpdateCollaboratorResult Ok() =>
        new(null, null);

    public static UpdateCollaboratorResult NotFound(string message = "Colaborador no encontrado.") =>
        new(UpdateCollaboratorError.NotFound, message);

    public static UpdateCollaboratorResult DuplicateDni(string message = "Ya existe otro colaborador registrado con ese DNI.") =>
        new(UpdateCollaboratorError.DuplicateDni, message);
}