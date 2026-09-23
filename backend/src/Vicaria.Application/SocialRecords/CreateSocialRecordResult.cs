namespace Vicaria.Application.SocialRecords;

public class CreateSocialRecordResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid PersonId { get; init; }
    public Guid SocialRecordId { get; init; }

    public static CreateSocialRecordResult Ok(Guid personId, Guid socialRecordId) => new()
    {
        Success = true,
        PersonId = personId,
        SocialRecordId = socialRecordId
    };
}
