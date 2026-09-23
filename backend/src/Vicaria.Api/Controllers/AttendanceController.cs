using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vicaria.Application.Attendances;
using Vicaria.Domain.Entities;

namespace Vicaria.Api.Controllers;

[ApiController]
[Route("api/attendance")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly IValidator<CreateAttendanceDto> _validator;

    public AttendanceController(
        IAttendanceService attendanceService,
        IValidator<CreateAttendanceDto> validator)
    {
        _attendanceService = attendanceService;
        _validator = validator;
    }

    private Guid ActorId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // registra la asistencia diaria de una persona (SCRUM-135) y reactiva su ficha si
    // estaba Inactiva; Escucha también puede cargarla en el día a día del Centro Barrial
    [HttpPost]
    [Authorize(Roles = $"{RoleNames.Referente},{RoleNames.DirectoraDeCasona},{RoleNames.CoordinadorDeCasaConvivencia},{RoleNames.Escucha}")]
    public async Task<IActionResult> Register([FromBody] CreateAttendanceDto dto, CancellationToken cancellationToken)
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

        var result = await _attendanceService.RegisterAsync(dto, ActorId, cancellationToken);

        return result.Error switch
        {
            null => NoContent(),
            RegisterAttendanceError.PersonNotFound => NotFound(new { message = result.ErrorMessage }),
            RegisterAttendanceError.SocialRecordNotFound => NotFound(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }
}