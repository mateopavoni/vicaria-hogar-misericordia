namespace Vicaria.Application.Activities;

public record ActivityDto(
    Guid Id,
    string Name,
    bool IsActive,
    bool IsFromCalendar,
    DateTime CreatedAt
);

public record CreateActivityDto(
    string Name
);

public record UpdateActivityDto(
    string Name,
    bool IsActive
);

public enum CreateActivityError
{
    DuplicateName
}

public record CreateActivityResult(
    Guid? ActivityId = null,
    CreateActivityError? Error = null,
    string? ErrorMessage = null
)
{
    public static CreateActivityResult Success(Guid id) => new(ActivityId: id);
    public static CreateActivityResult Duplicate(string message = "Ya existe una actividad activa con ese nombre.") =>
        new(Error: CreateActivityError.DuplicateName, ErrorMessage: message);
}

public enum UpdateActivityError
{
    NotFound,
    DuplicateName
}

public record UpdateActivityResult(
    UpdateActivityError? Error = null,
    string? ErrorMessage = null
)
{
    public bool IsSuccess => Error is null;
    public static UpdateActivityResult Success() => new();
    public static UpdateActivityResult NotFound(string message = "La actividad especificada no existe.") =>
        new(Error: UpdateActivityError.NotFound, ErrorMessage: message);
    public static UpdateActivityResult Duplicate(string message = "Ya existe otra actividad activa con ese nombre.") =>
        new(Error: UpdateActivityError.DuplicateName, ErrorMessage: message);
}

public enum DeleteActivityError
{
    NotFound
}

public record DeleteActivityResult(
    DeleteActivityError? Error = null,
    string? ErrorMessage = null
)
{
    public bool IsSuccess => Error is null;
    public static DeleteActivityResult Success() => new();
    public static DeleteActivityResult NotFound(string message = "La actividad especificada no existe.") =>
        new(Error: DeleteActivityError.NotFound, ErrorMessage: message);
}

public record GetActivityByIdResult(
    ActivityDto? Activity = null,
    string? ErrorMessage = null
)
{
    public bool IsSuccess => Activity is not null;
    public static GetActivityByIdResult Success(ActivityDto activity) => new(Activity: activity);
    public static GetActivityByIdResult NotFound(string message = "La actividad especificada no existe.") =>
        new(ErrorMessage: message);
}