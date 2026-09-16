namespace Vicaria.Application.SocialRecords;

public enum UpdatePersonStatusError
{
    PersonNotFound,
    SocialRecordNotFound
}

public class UpdatePersonStatusResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public UpdatePersonStatusError? Error { get; init; }

    public static UpdatePersonStatusResult Ok() => new() { Success = true };

    public static UpdatePersonStatusResult PersonNotFound() =>
        new() { Success = false, Error = UpdatePersonStatusError.PersonNotFound, ErrorMessage = "La persona no existe." };

    public static UpdatePersonStatusResult SocialRecordNotFound() =>
        new() { Success = false, Error = UpdatePersonStatusError.SocialRecordNotFound, ErrorMessage = "La persona no tiene una ficha social asociada." };
}