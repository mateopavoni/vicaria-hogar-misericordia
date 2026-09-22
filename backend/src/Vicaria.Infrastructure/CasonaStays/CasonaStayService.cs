using Microsoft.EntityFrameworkCore;
using Vicaria.Application.CasonaStays;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.Infrastructure.CasonaStays;

public class CasonaStayService : ICasonaStayService
{
    private readonly VicariaDbContext _dbContext;

    public CasonaStayService(VicariaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CasonaStayExitResult> ExitAsync(Guid stayId, CasonaStayExitDto dto, Guid actorId, CancellationToken cancellationToken = default)
    {
        var stay = await _dbContext.CasonaStays
            .FirstOrDefaultAsync(s => s.Id == stayId, cancellationToken);

        if (stay is null)
        {
            return CasonaStayExitResult.StayNotFound();
        }

        if (stay.ExitDate is not null)
        {
            return CasonaStayExitResult.AlreadyExited();
        }

        var now = DateTime.UtcNow;

        // SCRUM-146: fecha/hora de egreso automática, server-side (UTC)
        stay.ExitDate = now;
        stay.ExitReason = dto.ExitReason;
        stay.Reason = dto.Reason?.Trim();

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = "Egreso de estadía en Casona registrado",
            AffectedEntity = $"CasonaStay:{stay.Id}",
            Date = now
        });

        var socialRecord = await _dbContext.SocialRecords
            .FirstOrDefaultAsync(sr => sr.PersonId == stay.PersonId, cancellationToken);

        if (socialRecord is not null)
        {
            socialRecord.PersonType = PersonType.Ambulatory;
            socialRecord.Status = dto.NewStatus ?? SocialRecordStatus.Active;
            socialRecord.UpdatedAt = now;

            _dbContext.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = actorId,
                Action = $"Cambio de estado post-egreso a {socialRecord.PersonType} ({socialRecord.Status})",
                AffectedEntity = $"Person:{stay.PersonId}",
                Date = now
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return CasonaStayExitResult.Ok();
    }

    public async Task<IEnumerable<CasonaStayDto>> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var stays = await _dbContext.CasonaStays
            .AsNoTracking()
            .Where(s => s.PersonId == personId)
            .OrderByDescending(s => s.EntryDate)
            .ToListAsync(cancellationToken);

        return stays.Select(s =>
        {
            var endDate = s.ExitDate ?? now;
            var duration = (int)(endDate - s.EntryDate).TotalDays;

            return new CasonaStayDto
            {
                Id = s.Id,
                PersonId = s.PersonId,
                EntryDate = s.EntryDate,
                ExitDate = s.ExitDate,
                ExitReason = s.ExitReason,
                Reason = s.Reason,
                DurationDays = Math.Max(0, duration)
            };
        });
    }
}