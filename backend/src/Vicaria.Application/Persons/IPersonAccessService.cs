namespace Vicaria.Application.Persons;

// La Directora de Casa de Convivencia solo accede a Residentes. La regla
// debe valer para cualquier recurso de la persona, no solo para el listado de fichas.
public interface IPersonAccessService
{
    Task<bool> CanAccessPersonAsync(Guid personId, string? role, CancellationToken cancellationToken = default);
    Task<bool> CanAccessSocialRecordAsync(Guid socialRecordId, string? role, CancellationToken cancellationToken = default);
}
