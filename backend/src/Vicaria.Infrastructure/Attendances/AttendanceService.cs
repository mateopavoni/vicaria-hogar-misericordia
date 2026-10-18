using Microsoft.EntityFrameworkCore;
using Vicaria.Application.Attendances;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.Infrastructure.Attendances;

public class AttendanceService : IAttendanceService
{
    private readonly VicariaDbContext _dbContext;

    public AttendanceService(VicariaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RegisterAttendanceResult> RegisterAsync(CreateAttendanceDto dto, Guid actorId, CancellationToken cancellationToken = default)
    {
        var person = await _dbContext.People
            .FirstOrDefaultAsync(p => p.Id == dto.PersonId, cancellationToken);
        if (person is null)
        {
            return RegisterAttendanceResult.PersonNotFound();
        }

        var socialRecord = await _dbContext.SocialRecords
            .FirstOrDefaultAsync(r => r.PersonId == dto.PersonId, cancellationToken);
        if (socialRecord is null)
        {
            return RegisterAttendanceResult.SocialRecordNotFound();
        }

        // SCRUM-216: la unicidad es por día (persona, actividad, fecha); se normaliza
        // la fecha a medianoche para que el índice único compuesto la compare por día
        var attendanceDate = dto.Date.Date;

        var alreadyRegistered = await _dbContext.Attendances
            .AnyAsync(a => a.PersonId == dto.PersonId && a.Activity == dto.Activity && a.Date == attendanceDate, cancellationToken);
        if (alreadyRegistered)
        {
            return RegisterAttendanceResult.Duplicate();
        }

        _dbContext.Attendances.Add(new Attendance
        {
            Id = Guid.NewGuid(),
            PersonId = dto.PersonId,
            Date = attendanceDate,
            Activity = dto.Activity,
            AnonymousCount = dto.AnonymousCount,
            CreatedByUserId = actorId
        });

        // SCRUM-135: una nueva asistencia reactiva la ficha si había quedado Inactiva
        if (socialRecord.Status == SocialRecordStatus.Inactive)
        {
            socialRecord.Status = SocialRecordStatus.Active;
            socialRecord.UpdatedAt = DateTime.UtcNow;

            _dbContext.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = actorId,
                Action = "Persona reactivada por nueva asistencia",
                AffectedEntity = $"SocialRecord:{socialRecord.Id}",
                Date = DateTime.UtcNow
            });
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // el índice único compuesto (persona, actividad, fecha) rechaza el duplicado
            // ante una condición de carrera entre el chequeo previo y el guardado
            return RegisterAttendanceResult.Duplicate();
        }

        return RegisterAttendanceResult.Ok();
    }
}