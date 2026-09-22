namespace Vicaria.Application.Observations;

public record ObservationCategoryDto(Guid Id, string Name, bool IsActive, DateTime CreatedAt);
public record CreateObservationCategoryDto(string Name);
public record UpdateObservationCategoryDto(string Name);
public record ToggleObservationCategoryStatusDto(bool IsActive);

public enum CategoryOperationError { NotFound, DuplicateName }

public record CategoryOperationResult(bool Success, ObservationCategoryDto? Data = null, CategoryOperationError? Error = null, string? ErrorMessage = null)
{
    public static CategoryOperationResult Ok(ObservationCategoryDto data) => new(true, Data: data);
    public static CategoryOperationResult Ok() => new(true);
    public static CategoryOperationResult NotFound(string message = "La categoría especificada no existe.") => new(false, Error: CategoryOperationError.NotFound, ErrorMessage: message);
    public static CategoryOperationResult DuplicateName(string message = "Ya existe una categoría con ese nombre.") => new(false, Error: CategoryOperationError.DuplicateName, ErrorMessage: message);
}