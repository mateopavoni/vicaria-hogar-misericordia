namespace Vicaria.Application.MedicationSchemes;

public interface IMedicationSchemeService
{
    Task<CreateMedicationSchemeResult> CreateAsync(Guid personId, CreateMedicationSchemeDto dto, Guid actorId, CancellationToken cancellationToken = default);
    Task<GetPersonMedicationSchemesResult> GetByPersonIdAsync(Guid personId, bool? onlyActive = null, bool? needsReview = null, CancellationToken cancellationToken = default);
    // SCRUM-249: esquemas vigentes que llevan más de 30 días sin revisión, de todas las personas
    Task<IReadOnlyList<MedicationReviewAlertDto>> GetNeedsReviewAsync(CancellationToken cancellationToken = default);
    Task<DeactivateMedicationSchemeResult> DeactivateAsync(Guid personId, Guid schemeId, Guid actorId, CancellationToken cancellationToken = default);
    // SCRUM-249: registra la revisión del esquema (ReviewedAt + ReviewedByUserId)
    Task<ReviewMedicationSchemeResult> ReviewAsync(Guid personId, Guid schemeId, Guid actorId, CancellationToken cancellationToken = default);
}
