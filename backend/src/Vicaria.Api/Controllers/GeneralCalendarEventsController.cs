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
    private readonly IValidator<UpdateGeneralCalendarEventDto> _updateValidator;

    public GeneralCalendarEventsController(
        IGeneralCalendarEventService calendarEventService,
        IValidator<CreateGeneralCalendarEventDto> validator,
        IValidator<UpdateGeneralCalendarEventDto> updateValidator)
    {
        _calendarEventService = calendarEventService;
        _validator = validator;
        _updateValidator = updateValidator;
    }

    private Guid ActorId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsReferent => User.IsInRole(RoleNames.Referent);

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

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateGeneralCalendarEventDto dto,
        CancellationToken cancellationToken)
    {
        var validationResult = await _updateValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return ValidationProblem(ModelState);
        }

        var result = await _calendarEventService.UpdateAsync(id, dto, ActorId, IsReferent, cancellationToken);
        return result.Error switch
        {
            null => NoContent(),
            CalendarEventOperationError.NotFound => NotFound(new { message = result.ErrorMessage }),
            CalendarEventOperationError.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _calendarEventService.DeleteAsync(id, ActorId, IsReferent, cancellationToken);
        return result.Error switch
        {
            null => NoContent(),
            CalendarEventOperationError.NotFound => NotFound(new { message = result.ErrorMessage }),
            CalendarEventOperationError.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
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
