using Microsoft.EntityFrameworkCore;
using Vicaria.Application.CasonaVisits;
using Vicaria.Application.Common;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.Infrastructure.CasonaVisits;

public class CasonaVisitService : ICasonaVisitService
{
    private const int PageSize = 10;

    private readonly VicariaDbContext _dbContext;

    public CasonaVisitService(VicariaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CreateCasonaVisitResult> CreateAsync(
        CreateCasonaVisitDto dto,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        var date = dto.Date.Date;

        var personIssue = await CheckPersonAsync(dto.PersonId, cancellationToken);
        if (personIssue is not PersonIssue.None)
        {
            return MapCreatePersonIssue(personIssue);
        }

        if (!dto.AllowOverlap
            && await HasOverlappingVisitAsync(date, dto.StartTime, dto.EstimatedDurationMinutes, null, cancellationToken))
        {
            return CreateCasonaVisitResult.TimeOverlap();
        }

        var visit = new CasonaVisit
        {
            Id = Guid.NewGuid(),
            PersonId = dto.PersonId,
            VisitorName = dto.VisitorName.Trim(),
            Date = date,
            StartTime = dto.StartTime,
            EstimatedDurationMinutes = dto.EstimatedDurationMinutes,
            Status = VisitStatus.Pending,
            CreatedByUserId = actorId,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.CasonaVisits.Add(visit);

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = "Visita creada",
            AffectedEntity = $"CasonaVisit:{visit.Id}",
            Date = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreateCasonaVisitResult.Ok(visit.Id);
    }

    public async Task<UpdateCasonaVisitResult> UpdateAsync(
        Guid casonaVisitId,
        UpdateCasonaVisitDto dto,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        var visit = await _dbContext.CasonaVisits
            .FirstOrDefaultAsync(v => v.Id == casonaVisitId, cancellationToken);

        if (visit is null)
        {
            return UpdateCasonaVisitResult.NotFound();
        }

        // solo una visita Pendiente es editable; Realizada/Cancelada quedan congeladas (SCRUM-210)
        if (visit.Status != VisitStatus.Pending)
        {
            return UpdateCasonaVisitResult.InvalidState();
        }

        var date = dto.Date.Date;

        var personIssue = await CheckPersonAsync(dto.PersonId, cancellationToken);
        if (personIssue is not PersonIssue.None)
        {
            return MapUpdatePersonIssue(personIssue);
        }

        // la regla de "no en el pasado" solo aplica si cambiaron fecha u hora: si no,
        // no sería posible marcar como Realizada una visita de hoy ya ocurrida
        var scheduleChanged = date != visit.Date.Date || dto.StartTime != visit.StartTime;
        if (scheduleChanged && date + dto.StartTime < DateTime.UtcNow)
        {
            return UpdateCasonaVisitResult.VisitInPast();
        }

        if (!dto.AllowOverlap
            && await HasOverlappingVisitAsync(date, dto.StartTime, dto.EstimatedDurationMinutes, visit.Id, cancellationToken))
        {
            return UpdateCasonaVisitResult.TimeOverlap();
        }

        visit.PersonId = dto.PersonId;
        visit.VisitorName = dto.VisitorName.Trim();
        visit.Date = date;
        visit.StartTime = dto.StartTime;
        visit.EstimatedDurationMinutes = dto.EstimatedDurationMinutes;
        visit.Status = dto.Status;
        visit.CancellationReason = dto.CancellationReason?.Trim();

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = "Visita actualizada",
            AffectedEntity = $"CasonaVisit:{visit.Id}",
            Date = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return UpdateCasonaVisitResult.Ok();
    }

    public async Task<PagedResult<CasonaVisitListItemDto>> GetByRangeAsync(
        DateTime from,
        DateTime to,
        int page,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        var fromDate = from.Date;
        var toDate = to.Date;

        var visits = await _dbContext.CasonaVisits
            .AsNoTracking()
            .Include(v => v.Person)
            .Where(v => v.Date >= fromDate && v.Date <= toDate)
            .OrderBy(v => v.Date)
            .ThenBy(v => v.StartTime)
            .ToListAsync(cancellationToken);

        var total = visits.Count;
        var items = visits
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(v => new CasonaVisitListItemDto(
                v.Id,
                v.PersonId,
                v.Person is null
                    ? string.Empty
                    : $"{v.Person.FirstName} {v.Person.LastName}".Trim(),
                v.VisitorName,
                v.Date,
                v.StartTime,
                v.EstimatedDurationMinutes,
                v.Status,
                v.CancellationReason))
            .ToList();

        return new PagedResult<CasonaVisitListItemDto>(items, total, (int)Math.Ceiling(total / (double)PageSize));
    }

    public async Task<ChangeCasonaVisitStatusResult> ChangeStatusAsync(
        Guid casonaVisitId,
        ChangeCasonaVisitStatusDto dto,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        var visit = await _dbContext.CasonaVisits
            .FirstOrDefaultAsync(v => v.Id == casonaVisitId, cancellationToken);

        if (visit is null)
        {
            return ChangeCasonaVisitStatusResult.NotFound();
        }

        visit.Status = dto.Status;
        visit.CancellationReason = dto.Status == VisitStatus.Cancelled 
            ? dto.CancellationReason?.Trim() 
            : null;

        var actionText = dto.Status switch
        {
            VisitStatus.Completed => "Visita marcada como realizada",
            VisitStatus.Cancelled => "Visita cancelada",
            _ => "Visita marcada como pendiente"
        };

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = actionText,
            AffectedEntity = $"CasonaVisit:{visit.Id}",
            Date = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ChangeCasonaVisitStatusResult.Ok();
    }
    
    private enum PersonIssue
    {
        None,
        PersonNotFound,
        NotResident,
        NoOpenStay
    }

    // la persona debe existir, tener ficha con tipo Residente y una estadía sin egreso
    // (reglas de SCRUM-210: visitas de residentes a la Casa de Convivencia)
    private async Task<PersonIssue> CheckPersonAsync(Guid personId, CancellationToken cancellationToken)
    {
        var person = await _dbContext.People
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == personId, cancellationToken);

        if (person is null)
        {
            return PersonIssue.PersonNotFound;
        }

        var record = await _dbContext.SocialRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.PersonId == personId, cancellationToken);

        if (record?.PersonType != PersonType.Resident)
        {
            return PersonIssue.NotResident;
        }

        var hasOpenStay = await _dbContext.CasaConvivenciaStays
            .AnyAsync(s => s.PersonId == personId && s.ExitDate == null, cancellationToken);

        return hasOpenStay ? PersonIssue.None : PersonIssue.NoOpenStay;
    }

    // solape general: ninguna visita (de cualquier persona) puede pisarse en fecha y horario;
    // excludeVisitId deja afuera a la que se está editando
    private async Task<bool> HasOverlappingVisitAsync(
        DateTime date,
        TimeSpan startTime,
        int estimatedDurationMinutes,
        Guid? excludeVisitId,
        CancellationToken cancellationToken)
    {
        var visits = _dbContext.CasonaVisits
            .AsNoTracking()
            .Where(v => v.Date == date && v.Status != VisitStatus.Cancelled);

        if (excludeVisitId.HasValue)
        {
            visits = visits.Where(v => v.Id != excludeVisitId.Value);
        }

        var candidates = await visits.ToListAsync(cancellationToken);

        var start = startTime;
        var end = startTime + TimeSpan.FromMinutes(estimatedDurationMinutes);

        return candidates.Any(v =>
            start < v.StartTime + TimeSpan.FromMinutes(v.EstimatedDurationMinutes) &&
            v.StartTime < end);
    }

    private static CreateCasonaVisitResult MapCreatePersonIssue(PersonIssue issue) => issue switch
    {
        PersonIssue.PersonNotFound => CreateCasonaVisitResult.PersonNotFound(),
        PersonIssue.NotResident => CreateCasonaVisitResult.PersonNotResident(),
        _ => CreateCasonaVisitResult.NoOpenStay()
    };

    private static UpdateCasonaVisitResult MapUpdatePersonIssue(PersonIssue issue) => issue switch
    {
        PersonIssue.PersonNotFound => UpdateCasonaVisitResult.PersonNotFound(),
        PersonIssue.NotResident => UpdateCasonaVisitResult.PersonNotResident(),
        _ => UpdateCasonaVisitResult.NoOpenStay()
    };
}
