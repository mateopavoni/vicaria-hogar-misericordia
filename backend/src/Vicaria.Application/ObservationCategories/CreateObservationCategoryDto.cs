namespace Vicaria.Application.ObservationCategories;

// categoría de observación (SCRUM-176): Name obligatorio, Description opcional
public record CreateObservationCategoryDto(string Name, string? Description);