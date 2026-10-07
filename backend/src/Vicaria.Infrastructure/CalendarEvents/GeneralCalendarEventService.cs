using Vicaria.Application.CalendarEvents;
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
}
