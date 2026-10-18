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
            return RegisterAttendanceResult.Duplicate();
        }

        return RegisterAttendanceResult.Ok();
    }

    public async Task<GetPersonAttendanceHistoryResult> GetPersonAttendanceHistoryAsync(Guid personId,GetPersonAttendanceHistoryFilterDto filter,Guid actorId,IEnumerable<string> actorRoles,CancellationToken cancellationToken = default)
    {
        var personExists = await _dbContext.People
            .AsNoTracking()
            .AnyAsync(p => p.Id == personId, cancellationToken);

        if (!personExists)
        {
            return GetPersonAttendanceHistoryResult.NotFound();
        }

        var isDirector = actorRoles.Contains(RoleNames.CasaConvivenciaDirector);
        var isReferentOrCoordinator = actorRoles.Contains(RoleNames.Referent) ||
                                      actorRoles.Contains(RoleNames.CasaConvivenciaCoordinator);
        if (isDirector && !isReferentOrCoordinator)
        {
            var isCasonaResident = await _dbContext.CasaConvivenciaStays
                .AsNoTracking()
                .AnyAsync(s => s.PersonId == personId && s.ExitDate == null, cancellationToken);

            if (!isCasonaResident)
            {
                return GetPersonAttendanceHistoryResult.Forbidden("La Directora de Casona solo puede consultar el historial de residentes activos de Casona.");
            }
        }

        var query = _dbContext.Attendances
            .AsNoTracking()
            .Where(a => a.PersonId == personId);

        if (filter.Activity.HasValue)
        {
            query = query.Where(a => a.Activity == filter.Activity.Value);
        }

        if (filter.FromDate.HasValue)
        {
            var from = filter.FromDate.Value.Date;
            query = query.Where(a => a.Date >= from);
        }

        if (filter.ToDate.HasValue)
        {
            var to = filter.ToDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(a => a.Date <= to);
        }

        var attendances = await query
            .OrderByDescending(a => a.Date)
            .ToListAsync(cancellationToken);

        var groups = attendances
            .GroupBy(a => a.Activity)
            .Select(g => new ActivityAttendanceGroupDto(
                Activity: g.Key,
                ActivityName: g.Key switch
                {
                    AttendanceActivity.DayCenter => "Centro de Día",
                    AttendanceActivity.CasaConvivencia => "Casa de Convivencia",
                    _ => g.Key.ToString()
                },
                Attendances: g.Select(a => new PersonAttendanceItemDto(
                    Id: a.Id,
                    Date: a.Date,
                    AnonymousCount: a.AnonymousCount
                )).ToList()
            ))
            .ToList();

        var dto = new PersonAttendanceHistoryDto(
            PersonId: personId,
            Groups: groups
        );

        return GetPersonAttendanceHistoryResult.Success(dto);
    }
}