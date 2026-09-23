using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vicaria.Application.CasaConvivenciaStays;
using Vicaria.Domain.Entities;

namespace Vicaria.Api.Controllers;

[ApiController]
[Route("api/casa-convivencia-stays")]
[Authorize]
public class CasaConvivenciaStayController : ControllerBase
{
    private readonly ICasaConvivenciaStayService _casaConvivenciaStayService;
    private readonly IValidator<CasaConvivenciaStayExitDto> _exitValidator;

    public CasaConvivenciaStayController(
        ICasaConvivenciaStayService casaConvivenciaStayService,
        IValidator<CasaConvivenciaStayExitDto> exitValidator)
    {
        _casaConvivenciaStayService = casaConvivenciaStayService;
        _exitValidator = exitValidator;
    }

    private Guid ActorId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // registra el egreso de una estadía (SCRUM-146): fecha/hora automática server-side +
    // motivo opcional (enum) + texto libre solo si motivo = otro
    [HttpPut("{id}/egreso")]
    [Authorize(Roles = RoleNames.Referente)]
    public async Task<IActionResult> Exit(Guid id, [FromBody] CasaConvivenciaStayExitDto dto, CancellationToken cancellationToken)
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

        var result = await _casaConvivenciaStayService.ExitAsync(id, dto, ActorId, cancellationToken);

        return result.Error switch
        {
            null => NoContent(),
            CasaConvivenciaStayExitError.StayNotFound => NotFound(new { message = result.ErrorMessage }),
            CasaConvivenciaStayExitError.AlreadyExited => BadRequest(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }
}