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

    public async Task<DailyAttendanceDto> GetDailyAsync(AttendanceActivity activity, DateTime date, CancellationToken cancellationToken = default)
    {
        var day = date.Date;
        var rows = await _dbContext.Attendances
            .AsNoTracking()
            .Where(a => a.Activity == activity && a.Date == day)
            .Select(a => new
            {
                a.Id,
                a.PersonId,
                a.AnonymousCount,
                a.Person!.FirstName,
                a.Person.LastName,
                a.Person.Dni
            })
            .ToListAsync(cancellationToken);

        var persons = rows
            .Select(r => new DailyAttendancePersonDto(r.Id, r.PersonId, $"{r.FirstName} {r.LastName}".Trim(), r.Dni, r.AnonymousCount))
            .OrderBy(p => p.FullName)
            .ToList();

        return new DailyAttendanceDto(activity, day, rows.Sum(r => r.AnonymousCount), persons);
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

        // Generación automática de agenda diaria de medicamentos según asistencia (SCRUM-236)
        var activeSchemes = await _dbContext.MedicationSchemes
            .Where(s => s.PersonId == dto.PersonId && s.IsActive)
            .ToListAsync(cancellationToken);

        if (activeSchemes.Count > 0)
        {
            var existingSchemeIds = await _dbContext.DailyMedicationSchedules
                .Where(d => d.PersonId == dto.PersonId && d.Date == attendanceDate)
                .Select(d => d.MedicationSchemeId)
                .ToListAsync(cancellationToken);

            var existingSchemeIdSet = existingSchemeIds.ToHashSet();
            var newIntakesCreated = 0;

            foreach (var scheme in activeSchemes)
            {
                if (existingSchemeIdSet.Contains(scheme.Id))
                {
                    continue;
                }

                _dbContext.DailyMedicationSchedules.Add(new DailyMedicationSchedule
                {
                    Id = Guid.NewGuid(),
                    PersonId = dto.PersonId,
                    MedicationSchemeId = scheme.Id,
                    MedicationId = scheme.MedicationId,
                    Date = attendanceDate,
                    Time = scheme.Time,
                    Dose = scheme.Dose,
                    Status = MedicationIntakeStatus.Pending
                });

                newIntakesCreated++;
            }

            if (newIntakesCreated > 0)
            {
                _dbContext.AuditLogs.Add(new AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = actorId,
                    Action = "Agenda diaria de medicamentos generada por asistencia",
                    AffectedEntity = $"Person:{dto.PersonId}",
                    Date = DateTime.UtcNow
                });
            }
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

    public async Task<UpdateAttendanceResult> UpdateAsync(Guid attendanceId, UpdateAttendanceDto dto, Guid actorId, CancellationToken cancellationToken = default)
    {
        var attendance = await _dbContext.Attendances
            .FirstOrDefaultAsync(a => a.Id == attendanceId, cancellationToken);
        if (attendance is null)
        {
            return UpdateAttendanceResult.NotFound();
        }

        // día cerrado (SCRUM-217): fecha anterior a hoy; la justificación es obligatoria
        var isClosedDay = attendance.Date.Date < DateTime.UtcNow.Date;
        var justification = dto.Justification?.Trim();
        if (isClosedDay && string.IsNullOrWhiteSpace(justification))
        {
            return UpdateAttendanceResult.JustificationRequired();
        }

        if (dto.AnonymousCount.HasValue)
        {
            attendance.AnonymousCount = dto.AnonymousCount.Value;
        }

        if (!string.IsNullOrWhiteSpace(justification))
        {
            attendance.Justification = justification;
        }

        var action = isClosedDay
            ? $"Modificación de asistencia de día cerrado con justificación: {justification}"
            : "Modificación de asistencia";
        if (action.Length > 500)
        {
            action = action[..500];
        }

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = action,
            AffectedEntity = $"Attendance:{attendance.Id}",
            Date = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return UpdateAttendanceResult.Ok();
    }

    public async Task<GetPersonAttendanceHistoryResult> GetPersonAttendanceHistoryAsync(Guid personId, GetPersonAttendanceHistoryFilterDto filter, Guid actorId, IEnumerable<string> actorRoles, CancellationToken cancellationToken = default)
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
        var allPersonDates = await _dbContext.Attendances
            .AsNoTracking()
            .Where(a => a.PersonId == personId)
            .Select(a => a.Date.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToListAsync(cancellationToken);

        var today = DateTime.UtcNow.Date;
        var totalDiasAsistidos = allPersonDates.Count;
        DateTime? ultimaAsistencia = allPersonDates.Count > 0 ? allPersonDates[0] : null;

        var sinAsistir7Dias = ultimaAsistencia is null || (today - ultimaAsistencia.Value).TotalDays > 7;

        var rachaActual = 0;
        if (allPersonDates.Count > 0)
        {
            var dateSet = allPersonDates.ToHashSet();
            var checkDate = dateSet.Contains(today) ? today : (dateSet.Contains(today.AddDays(-1)) ? today.AddDays(-1) : (DateTime?)null);

            if (checkDate.HasValue)
            {
                var cursor = checkDate.Value;
                while (dateSet.Contains(cursor))
                {
                    rachaActual++;
                    cursor = cursor.AddDays(-1);
                }
            }
        }

        var indicators = new PersonAttendanceIndicatorsDto(
            TotalDiasAsistidos: totalDiasAsistidos,
            RachaActual: rachaActual,
            UltimaAsistencia: ultimaAsistencia,
            SinAsistir7Dias: sinAsistir7Dias
        );

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
            Indicators: indicators,
            Groups: groups
        );
        return GetPersonAttendanceHistoryResult.Success(dto);
    }
}