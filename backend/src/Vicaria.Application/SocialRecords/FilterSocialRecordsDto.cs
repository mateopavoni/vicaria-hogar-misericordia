namespace Vicaria.Application.SocialRecords;

public record FilterSocialRecordsDto(
    DateTime? EntryDateFrom = null,
    DateTime? EntryDateTo = null,
    int? DaysWithoutObservations = null,
    bool? HasDni = null,
    bool? HasAddress = null
);