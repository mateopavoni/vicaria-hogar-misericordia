using Vicaria.Domain.Entities;

namespace Vicaria.Application.CaritasCatalogs;

public record CaritasCatalogValueDto(Guid Id, CaritasCatalogCategory Category, string Name, string? Description, bool IsActive, DateTime CreatedAt);
public record CreateCaritasCatalogValueDto(CaritasCatalogCategory Category, string Name, string? Description = null);
// la categoría se fija en la creación y no se modifica (conserva el sentido histórico del valor)
public record UpdateCaritasCatalogValueDto(string Name, string? Description = null);
public record ToggleCaritasCatalogValueStatusDto(bool IsActive);

public enum CaritasCatalogOperationError { NotFound, DuplicateName }

public record CaritasCatalogOperationResult(bool Success, CaritasCatalogValueDto? Data = null, CaritasCatalogOperationError? Error = null, string? ErrorMessage = null)
{
    public static CaritasCatalogOperationResult Ok(CaritasCatalogValueDto data) => new(true, Data: data);
    public static CaritasCatalogOperationResult Ok() => new(true);
    public static CaritasCatalogOperationResult NotFound(string message = "El valor especificado no existe.") => new(false, Error: CaritasCatalogOperationError.NotFound, ErrorMessage: message);
    public static CaritasCatalogOperationResult DuplicateName(string message = "Ya existe un valor con ese nombre en esta categoría.") => new(false, Error: CaritasCatalogOperationError.DuplicateName, ErrorMessage: message);
}
