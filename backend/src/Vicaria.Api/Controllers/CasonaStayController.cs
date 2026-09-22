using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vicaria.Application.CasonaStays;
using Vicaria.Domain.Entities;

namespace Vicaria.Api.Controllers;

[ApiController]
[Route("api/casona-stays")]
[Authorize]
public class CasonaStayController : ControllerBase
{
    private readonly ICasonaStayService _casonaStayService;
    private readonly IValidator<CasonaStayExitDto> _exitValidator;

    public CasonaStayController(
        ICasonaStayService casonaStayService,
        IValidator<CasonaStayExitDto> exitValidator)
    {
        _casonaStayService = casonaStayService;
        _exitValidator = exitValidator;
    }

    private Guid ActorId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // registra el egreso de una estadía (SCRUM-146): fecha/hora automática server-side +
    // motivo opcional (enum) + texto libre solo si motivo = otro
    [HttpPut("{id}/egreso")]
    [Authorize(Roles = RoleNames.Referente)]
    public async Task<IActionResult> Exit(Guid id, [FromBody] CasonaStayExitDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await _exitValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return ValidationProblem(ModelState);
        }

        var result = await _casonaStayService.ExitAsync(id, dto, ActorId, cancellationToken);

        return result.Error switch
        {
            null => NoContent(),
            CasonaStayExitError.StayNotFound => NotFound(new { message = result.ErrorMessage }),
            CasonaStayExitError.AlreadyExited => BadRequest(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }
}