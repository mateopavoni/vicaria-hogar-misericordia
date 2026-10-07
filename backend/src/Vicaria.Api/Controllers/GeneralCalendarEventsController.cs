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
}
