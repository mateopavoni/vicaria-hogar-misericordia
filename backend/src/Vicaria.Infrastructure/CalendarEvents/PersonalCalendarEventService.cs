using Microsoft.EntityFrameworkCore;
using Vicaria.Application.CalendarEvents;
using Vicaria.Application.Common;
using Vicaria.Domain.Entities;
using Vicaria.Infrastructure.Persistence;

namespace Vicaria.Infrastructure.CalendarEvents;

public class PersonalCalendarEventService : IPersonalCalendarEventService
{
    private readonly VicariaDbContext _dbContext;

    public PersonalCalendarEventService(VicariaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> CreateAsync(
        CreateGeneralCalendarEventDto dto,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        // el autor sale del JWT, nunca del cliente: es lo que hace privado el evento (SCRUM-194)
        var calendarEvent = new PersonalCalendarEvent
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
        _dbContext.PersonalCalendarEvents.Add(calendarEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return calendarEvent.Id;
    }

    public async Task<PagedResult<CalendarEventOccurrenceDto>> GetOccurrencesAsync(
        DateTime from,
        DateTime to,
        int page,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        const int pageSize = 10;
        page = Math.Max(1, page);
        var fromDate = from.Date;
        var toDate = to.Date;

        // el filtro por autor es la garantía de privacidad (SCRUM-194): los eventos de
        // otro usuario jamás se materializan, aunque el rol tenga acceso al endpoint
        var calendarEvents = await _dbContext.PersonalCalendarEvents
            .AsNoTracking()
            .Where(e => e.AuthorUserId == actorId)
            .Where(e => e.Date == null || (e.Date >= fromDate && e.Date <= toDate))
            .ToListAsync(cancellationToken);

        var occurrences = calendarEvents
            .SelectMany(e => CalendarEventOccurrenceExpander
                .Expand(e.Date, e.RecurrenceDays, fromDate, toDate)
                .Select(d => new CalendarEventOccurrenceDto(
                    e.Id, d, e.StartTime, e.EndTime, e.Title, e.Description, e.AuthorUserId)))
            .OrderBy(o => o.Date)
            .ThenBy(o => o.StartTime)
            .ToList();

        var total = occurrences.Count;
        var items = occurrences.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResult<CalendarEventOccurrenceDto>(items, total, (int)Math.Ceiling(total / (double)pageSize));
    }

    public async Task<PersonalCalendarEventDetailDto?> GetByIdAsync(
        Guid id,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        // evento ajeno o inexistente devuelven lo mismo: el controller responde 404 en
        // ambos casos para no revelar que el evento existe
        var calendarEvent = await _dbContext.PersonalCalendarEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id && e.AuthorUserId == actorId, cancellationToken);

        if (calendarEvent is null)
        {
            return null;
        }

        return new PersonalCalendarEventDetailDto(
            calendarEvent.Id,
            calendarEvent.Title,
            calendarEvent.Description,
            calendarEvent.Date,
            calendarEvent.StartTime,
            calendarEvent.EndTime,
            calendarEvent.RecurrenceDays,
            calendarEvent.AuthorUserId,
            calendarEvent.CreatedAt);
    }
}
