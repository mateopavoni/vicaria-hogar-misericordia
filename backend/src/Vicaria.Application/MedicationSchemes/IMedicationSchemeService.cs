namespace Vicaria.Application.MedicationSchemes;

public interface IMedicationSchemeService
{
    Task<CreateMedicationSchemeResult> CreateAsync(
        Guid personId,
        CreateMedicationSchemeDto dto,
        Guid actorId,
        CancellationToken cancellationToken);
}
