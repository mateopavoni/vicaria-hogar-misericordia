using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vicaria.Application.Attendances;
using Vicaria.Domain.Entities;

namespace Vicaria.Api.Controllers;

[ApiController]
[Route("api/personas/{id:guid}/asistencias")]
[Authorize(Roles = $"{RoleNames.Referent},{RoleNames.CasaConvivenciaDirector},{RoleNames.CasaConvivenciaCoordinator}")]
public class PersonAttendancesController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly IValidator<GetPersonAttendanceHistoryFilterDto> _validator;

    public PersonAttendancesController(
        IAttendanceService attendanceService,
        IValidator<GetPersonAttendanceHistoryFilterDto> validator)
    {
        _attendanceService = attendanceService;
        _validator = validator;
    }

    private Guid ActorId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private IEnumerable<string> ActorRoles => User.FindAll(ClaimTypes.Role).Select(c => c.Value);

    [HttpGet]
    public async Task<IActionResult> GetHistory([FromRoute] Guid id,[FromQuery] AttendanceActivity? actividad,[FromQuery] DateTime? fechaDesde,[FromQuery] DateTime? fechaHasta,CancellationToken cancellationToken)
    {
        var filter = new GetPersonAttendanceHistoryFilterDto(actividad, fechaDesde, fechaHasta);
        var validationResult = await _validator.ValidateAsync(filter, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return ValidationProblem(ModelState);
        }

        var result = await _attendanceService.GetPersonAttendanceHistoryAsync(
            id,
            filter,
            ActorId,
            ActorRoles,
            cancellationToken);

        return result.Error switch
        {
            null => Ok(result.Data),
            GetPersonAttendanceHistoryError.NotFound => NotFound(new { message = result.ErrorMessage }),
            GetPersonAttendanceHistoryError.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }
}