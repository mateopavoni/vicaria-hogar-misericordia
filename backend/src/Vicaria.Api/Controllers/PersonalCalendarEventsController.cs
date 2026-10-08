using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vicaria.Application.CalendarEvents;
using Vicaria.Domain.Entities;

namespace Vicaria.Api.Controllers;

[ApiController]
[Route("api/personal-calendar-events")]
[Authorize]
public class PersonalCalendarEventsController : ControllerBase
{
    private readonly IPersonalCalendarEventService _personalCalendarEventService;

    public PersonalCalendarEventsController(IPersonalCalendarEventService personalCalendarEventService)
    {
        _personalCalendarEventService = personalCalendarEventService;
    }

    private Guid ActorId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // los eventos personales solo los ve su creador (SCRUM-194): la protección es por
    // ownership, no por rol, por eso el filtrado va siempre contra el usuario del JWT
    [HttpGet]
    [Authorize(Roles = $"{RoleNames.Referent},{RoleNames.CasaConvivenciaDirector},{RoleNames.Listener},{RoleNames.CasaConvivenciaCoordinator}")]
    public async Task<IActionResult> GetOccurrences(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var fromDate = from?.Date ?? DateTime.UtcNow.Date;
        var toDate = to?.Date ?? fromDate.AddDays(6);
        if (fromDate > toDate)
        {
            return BadRequest(new { message = "El parámetro 'from' no puede ser posterior a 'to'." });
        }

        var result = await _personalCalendarEventService.GetOccurrencesAsync(
            fromDate, toDate, page, ActorId, cancellationToken);
        return Ok(result);
    }

    // evento personal ajeno responde igual que inexistente (404), sin revelar que existe
    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{RoleNames.Referent},{RoleNames.CasaConvivenciaDirector},{RoleNames.Listener},{RoleNames.CasaConvivenciaCoordinator}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var calendarEvent = await _personalCalendarEventService.GetByIdAsync(id, ActorId, cancellationToken);
        return calendarEvent is null
            ? NotFound(new { message = "El evento especificado no existe." })
            : Ok(calendarEvent);
    }

    [HttpPut("{id:guid}/publish")]
    [Authorize(Roles = $"{RoleNames.Referent},{RoleNames.CasaConvivenciaDirector},{RoleNames.Listener},{RoleNames.CasaConvivenciaCoordinator}")]
    public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken)
    {
        var result = await _personalCalendarEventService.PublishAsync(id, ActorId, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(new { message = result.ErrorMessage });
        }

        return Ok(new { id = result.GeneralCalendarEventId });
    }
}
