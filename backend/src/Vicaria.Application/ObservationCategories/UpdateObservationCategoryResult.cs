namespace Vicaria.Application.ObservationCategories;

public class UpdateObservationCategoryResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }

    public static UpdateObservationCategoryResult Ok() => new() { Success = true };

    public static UpdateObservationCategoryResult CategoryNotFound() =>
        new() { Success = false, ErrorMessage = "La categoría de observación no existe." };
}