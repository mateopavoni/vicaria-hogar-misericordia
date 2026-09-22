namespace Vicaria.Application.ObservationCategories;

public class CreateObservationCategoryResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid ObservationCategoryId { get; init; }

    public static CreateObservationCategoryResult Ok(Guid observationCategoryId) => new()
    {
        Success = true,
        ObservationCategoryId = observationCategoryId
    };
}