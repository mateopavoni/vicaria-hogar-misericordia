using Microsoft.EntityFrameworkCore;
using Vicaria.Application.Persons;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.Infrastructure.Persons;

public class PersonInactivityService : IPersonInactivityService
{
    private readonly VicariaDbContext _dbContext;

    public PersonInactivityService(VicariaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CheckAndProcessInactivityAsync(CancellationToken cancellationToken = default)
    {
        var thresholdDate = DateTime.UtcNow.AddDays(-30);

        var inactiveCandidates = await _dbContext.SocialRecords
            .Where(r => r.Status == SocialRecordStatus.Active && (r.UpdatedAt <= thresholdDate || (r.UpdatedAt == default && r.CreatedAt <= thresholdDate)))
            .ToListAsync(cancellationToken);

        if (inactiveCandidates.Count == 0) return;

        foreach (var record in inactiveCandidates)
        {
            record.Status = SocialRecordStatus.Inactive;
            record.UpdatedAt = DateTime.UtcNow;

            _dbContext.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = record.CreatedByUserId,
                Action = "Paso automático a inactivo por 30 días sin actividad",
                AffectedEntity = $"Person:{record.PersonId}",
                Date = DateTime.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}