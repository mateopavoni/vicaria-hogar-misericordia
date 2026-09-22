namespace Vicaria.Application.ObservationCategories;

// mismos campos editables que la creación (SCRUM-176)
public record UpdateObservationCategoryDto(string Name, string? Description);