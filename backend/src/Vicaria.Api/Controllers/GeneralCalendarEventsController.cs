using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vicaria.Application.CalendarEvents;
using Vicaria.Domain.Entities;

namespace Vicaria.Api.Controllers;

[ApiController]
[Route("api/general-calendar-events")]
[Authorize]
public class GeneralCalendarEventsController : ControllerBase
{
    private readonly IGeneralCalendarEventService _calendarEventService;
    private readonly IValidator<CreateGeneralCalendarEventDto> _validator;

    public GeneralCalendarEventsController(
        IGeneralCalendarEventService calendarEventService,
        IValidator<CreateGeneralCalendarEventDto> validator)
    {
        _calendarEventService = calendarEventService;
        _validator = validator;
    }

    private Guid ActorId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // alta solo para Referente (SCRUM-189); la visibilidad para todos los usuarios
    // la resuelve el listado/consulta del calendario, que es otra tarea
    [HttpPost]
    [Authorize(Roles = RoleNames.Referent)]
    public async Task<IActionResult> Create(
        [FromBody] CreateGeneralCalendarEventDto dto,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return ValidationProblem(ModelState);
        }

        var result = await _calendarEventService.CreateAsync(dto, ActorId, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { id = result.GeneralCalendarEventId });
    }

    // listado de eventos generales con ocurrencias recurrentes expandidas y paginado (SCRUM-194)
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

        var result = await _calendarEventService.GetOccurrencesAsync(fromDate, toDate, page, cancellationToken);
        return Ok(result);
    }

    // detalle de un evento general sin expandir; 404 si no existe (SCRUM-194)
    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{RoleNames.Referent},{RoleNames.CasaConvivenciaDirector},{RoleNames.Listener},{RoleNames.CasaConvivenciaCoordinator}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var calendarEvent = await _calendarEventService.GetByIdAsync(id, cancellationToken);
        return calendarEvent is null
            ? NotFound(new { message = "El evento especificado no existe." })
            : Ok(calendarEvent);
    }
}
