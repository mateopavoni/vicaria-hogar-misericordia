namespace Vicaria.Application.SocialRecords;

public class UpdateSocialRecordResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }

    public static UpdateSocialRecordResult Ok() => new() { Success = true };

    public static UpdateSocialRecordResult NotFound() =>
        new() { Success = false, ErrorMessage = "La ficha no existe." };
}
