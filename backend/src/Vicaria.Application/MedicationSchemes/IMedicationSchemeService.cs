namespace Vicaria.Application.MedicationSchemes;

public interface IMedicationSchemeService
{
    Task<CreateMedicationSchemeResult> CreateAsync(Guid personId, CreateMedicationSchemeDto dto, Guid actorId, CancellationToken cancellationToken = default);
    Task<GetPersonMedicationSchemesResult> GetByPersonIdAsync(Guid personId, bool? onlyActive = null, CancellationToken cancellationToken = default);
    Task<DeactivateMedicationSchemeResult> DeactivateAsync(Guid personId, Guid schemeId, Guid actorId, CancellationToken cancellationToken = default);
}