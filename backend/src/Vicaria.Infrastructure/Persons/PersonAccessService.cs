using Microsoft.EntityFrameworkCore;
using Vicaria.Application.Persons;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.Infrastructure.Persons;

public class PersonAccessService : IPersonAccessService
{
    private readonly VicariaDbContext _dbContext;

    public PersonAccessService(VicariaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Directora y Coordinador de Casa de Convivencia gestionan solo Residentes
    private static bool IsRestrictedToResidents(string? role) =>
        role == RoleNames.CasaConvivenciaDirector || role == RoleNames.CasaConvivenciaCoordinator;

    public async Task<bool> CanAccessPersonAsync(Guid personId, string? role, CancellationToken cancellationToken = default)
    {
        if (!IsRestrictedToResidents(role))
        {
            return true;
        }

        // si la ficha no existe se deja pasar: el endpoint responde 404 por su cuenta
        var record = await _dbContext.SocialRecords
            .AsNoTracking()
            .Where(r => r.PersonId == personId)
            .Select(r => new { r.PersonType })
            .FirstOrDefaultAsync(cancellationToken);

        return record is null || record.PersonType == PersonType.Resident;
    }

    public async Task<bool> CanAccessSocialRecordAsync(Guid socialRecordId, string? role, CancellationToken cancellationToken = default)
    {
        if (!IsRestrictedToResidents(role))
        {
            return true;
        }

        var record = await _dbContext.SocialRecords
            .AsNoTracking()
            .Where(r => r.Id == socialRecordId)
            .Select(r => new { r.PersonType })
            .FirstOrDefaultAsync(cancellationToken);

        return record is null || record.PersonType == PersonType.Resident;
    }
}
