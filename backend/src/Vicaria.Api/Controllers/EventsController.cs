using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vicaria.Application.Events;
using Vicaria.Domain.Entities;

namespace Vicaria.Api.Controllers;

[ApiController]
[Route("api/eventos")]
[Authorize]
public class EventsController : ControllerBase
{
    private const string CalendarViewRoles = $"{RoleNames.Referent},{RoleNames.CasaConvivenciaDirector},{RoleNames.Listener}";

    private readonly IEventService _eventService;

    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    [HttpGet]
    [Authorize(Roles = CalendarViewRoles)]
    public async Task<ActionResult<IReadOnlyList<EventOccurrenceDto>>> GetByRange(
        [FromQuery] DateTime desde,
        [FromQuery] DateTime hasta,
        CancellationToken cancellationToken)
    {
        if (desde > hasta)
        {
            return BadRequest(new { message = "El parámetro 'desde' no puede ser posterior a 'hasta'." });
        }

        var results = await _eventService.GetEventsByRangeAsync(desde, hasta, cancellationToken);
        return Ok(results);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = CalendarViewRoles)]
    public async Task<ActionResult<EventDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var ev = await _eventService.GetEventByIdAsync(id, cancellationToken);
        if (ev is null)
        {
            return NotFound(new { message = "El evento especificado no existe." });
        }

        return Ok(ev);
    }
}