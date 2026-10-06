using Microsoft.EntityFrameworkCore;
using Vicaria.Application.Attendances;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.Infrastructure.Attendances;

public class AttendanceInactivityService : IAttendanceInactivityService
{
    private readonly VicariaDbContext _dbContext;

    public AttendanceInactivityService(VicariaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CheckAndProcessInactivityAsync(CancellationToken cancellationToken = default)
    {
        // SCRUM-135: una persona queda Activa solo si registró asistencia en los últimos
        // 30 días; sin asistencia reciente (o sin ninguna asistencia) pasa a Inactive
        var thresholdDate = DateTime.UtcNow.AddDays(-30);

        var inactiveCandidates = await _dbContext.SocialRecords
            .Where(r => r.Status == SocialRecordStatus.Active
                && !_dbContext.Attendances.Any(a => a.PersonId == r.PersonId && a.Date >= thresholdDate))
            .ToListAsync(cancellationToken);

        if (inactiveCandidates.Count == 0)
        {
            return;
        }

        foreach (var record in inactiveCandidates)
        {
            record.Status = SocialRecordStatus.Inactive;
            record.UpdatedAt = DateTime.UtcNow;

            _dbContext.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = record.CreatedByUserId,
                Action = "Paso automático a inactivo por 30 días sin asistencia",
                AffectedEntity = $"SocialRecord:{record.Id}",
                Date = DateTime.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}