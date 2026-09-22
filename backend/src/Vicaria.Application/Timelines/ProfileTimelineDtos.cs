namespace Vicaria.Application.Timelines;

// proxies las entradas del timeline unificado del perfil (SCRUM-159): los
// hitos ya registrados en el expediente (estadías de Casona) y las observaciones
public enum ProfileTimelineEntryType
{
    Observation,
    CasonaStayEntry,
    CasonaStayExit
}

public record ProfileTimelineEntryDto(
    Guid Id,
    ProfileTimelineEntryType Type,
    DateTime Date,
    string Title,
    string? Content,
    Guid? CategoryId,
    string? CategoryName,
    string? AuthorName
);

public record ProfileTimelineResponseDto(
    IReadOnlyList<ProfileTimelineEntryDto> Items,
    int TotalCount
);

public enum ProfileTimelineError
{
    PersonNotFound
}

public record ProfileTimelineResult(
    bool Success,
    ProfileTimelineResponseDto? Data = null,
    ProfileTimelineError? Error = null,
    string? ErrorMessage = null
);