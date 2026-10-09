using Microsoft.EntityFrameworkCore;
using Vicaria.Application.CalendarEvents;
using Vicaria.Application.Common;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.Infrastructure.CalendarEvents;

public class GeneralCalendarEventService : IGeneralCalendarEventService
{
    private readonly VicariaDbContext _dbContext;

    public GeneralCalendarEventService(VicariaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CreateGeneralCalendarEventResult> CreateAsync(
        CreateGeneralCalendarEventDto dto,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        var calendarEvent = new GeneralCalendarEvent
        {
            Id = Guid.NewGuid(),
            Title = dto.Title.Trim(),
            Date = dto.Date,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            Description = dto.Description?.Trim(),
            RecurrenceDays = dto.RecurrenceDays,
            AuthorUserId = actorId,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.GeneralCalendarEvents.Add(calendarEvent);

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = "Evento general creado",
            AffectedEntity = $"GeneralCalendarEvent:{calendarEvent.Id}",
            Date = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreateGeneralCalendarEventResult.Ok(calendarEvent.Id);
    }

    public async Task<PagedResult<CalendarEventOccurrenceDto>> GetOccurrencesAsync(
        DateTime from,
        DateTime to,
        int page,
        CancellationToken cancellationToken)
    {
        const int pageSize = 10;
        page = Math.Max(1, page);
        var fromDate = from.Date;
        var toDate = to.Date;

        // las plantillas (Date null) viven fuera de la tabla de fechas: se evaluan siempre
        // y el rango las acota durante la expansion
        var calendarEvents = await _dbContext.GeneralCalendarEvents
            .AsNoTracking()
            .Include(e => e.AuthorUser)
            .Where(e => e.Date == null || (e.Date >= fromDate && e.Date <= toDate))
            .ToListAsync(cancellationToken);

        var occurrences = calendarEvents
            .SelectMany(e => CalendarEventOccurrenceExpander
                .Expand(e.Date, e.RecurrenceDays, fromDate, toDate)
                .Select(d => new CalendarEventOccurrenceDto(
                    e.Id, d, e.StartTime, e.EndTime, e.Title, e.Description,
                    e.AuthorUserId,
                    e.AuthorUser is null ? null : $"{e.AuthorUser.FirstName} {e.AuthorUser.LastName}".Trim(),
                    e.Date is null)))
            .OrderBy(o => o.Date)
            .ThenBy(o => o.StartTime)
            .ToList();

        var total = occurrences.Count;
        var items = occurrences.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResult<CalendarEventOccurrenceDto>(items, total, (int)Math.Ceiling(total / (double)pageSize));
    }

    public async Task<GeneralCalendarEventDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var calendarEvent = await _dbContext.GeneralCalendarEvents
            .AsNoTracking()
            .Include(e => e.AuthorUser)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (calendarEvent is null)
        {
            return null;
        }

        return new GeneralCalendarEventDetailDto(
            calendarEvent.Id,
            calendarEvent.Title,
            calendarEvent.Description,
            calendarEvent.Date,
            calendarEvent.StartTime,
            calendarEvent.EndTime,
            calendarEvent.RecurrenceDays,
            calendarEvent.AuthorUserId,
            calendarEvent.AuthorUser is null
                ? null
                : $"{calendarEvent.AuthorUser.FirstName} {calendarEvent.AuthorUser.LastName}".Trim(),
            calendarEvent.CreatedAt);
    }

    public async Task<CalendarEventOperationResult> UpdateAsync(
        Guid id,
        UpdateGeneralCalendarEventDto dto,
        Guid actorId,
        bool isReferent,
        CancellationToken cancellationToken)
    {
        var calendarEvent = await _dbContext.GeneralCalendarEvents
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (calendarEvent is null)
        {
            return CalendarEventOperationResult.NotFound();
        }

        if (calendarEvent.AuthorUserId != actorId && !isReferent)
        {
            return CalendarEventOperationResult.Forbidden();
        }

        calendarEvent.Title = dto.Title.Trim();
        calendarEvent.Description = dto.Description?.Trim();
        calendarEvent.Date = dto.Date;
        calendarEvent.StartTime = dto.StartTime;
        calendarEvent.EndTime = dto.EndTime;
        calendarEvent.RecurrenceDays = dto.RecurrenceDays;

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = "Evento general modificado",
            AffectedEntity = $"GeneralCalendarEvent:{calendarEvent.Id}",
            Date = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return CalendarEventOperationResult.Ok();
    }

    public async Task<CalendarEventOperationResult> DeleteAsync(
        Guid id,
        Guid actorId,
        bool isReferent,
        CancellationToken cancellationToken)
    {
        var calendarEvent = await _dbContext.GeneralCalendarEvents
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (calendarEvent is null)
        {
            return CalendarEventOperationResult.NotFound();
        }

        if (calendarEvent.AuthorUserId != actorId && !isReferent)
        {
            return CalendarEventOperationResult.Forbidden();
        }

        _dbContext.GeneralCalendarEvents.Remove(calendarEvent);

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = "Evento general eliminado",
            AffectedEntity = $"GeneralCalendarEvent:{calendarEvent.Id}",
            Date = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return CalendarEventOperationResult.Ok();
    }
}
