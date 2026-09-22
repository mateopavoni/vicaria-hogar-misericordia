namespace Vicaria.Application.ObservationCategories;

public enum DeactivateObservationCategoryError
{
    CategoryNotFound,
    AlreadyDeactivated
}

public class DeactivateObservationCategoryResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public DeactivateObservationCategoryError? Error { get; init; }

    public static DeactivateObservationCategoryResult Ok() => new() { Success = true };

    public static DeactivateObservationCategoryResult CategoryNotFound() =>
        new() { Success = false, Error = DeactivateObservationCategoryError.CategoryNotFound, ErrorMessage = "La categoría de observación no existe." };

    public static DeactivateObservationCategoryResult AlreadyDeactivated() =>
        new() { Success = false, Error = DeactivateObservationCategoryError.AlreadyDeactivated, ErrorMessage = "La categoría de observación ya se encuentra desactivada." };
}