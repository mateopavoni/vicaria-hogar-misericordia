using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vicaria.Application.MedicationAgendas;
using Vicaria.Domain.Entities;

namespace Vicaria.Api.Controllers;

[ApiController]
[Route("api/medicamentos/agenda")]
[Authorize(Roles = $"{RoleNames.Referent},{RoleNames.CasaConvivenciaDirector},{RoleNames.CasaConvivenciaCoordinator}")]
public class MedicationsAgendaController : ControllerBase
{
    private readonly IMedicationAgendaService _agendaService;
    private readonly IValidator<UpdateMedicationIntakeStatusDto> _updateValidator;

    public MedicationsAgendaController(
        IMedicationAgendaService agendaService,
        IValidator<UpdateMedicationIntakeStatusDto> updateValidator)
    {
        _agendaService = agendaService;
        _updateValidator = updateValidator;
    }

    private Guid ActorId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetDailyAgenda([FromQuery] DateTime? fecha, CancellationToken cancellationToken)
    {
        var agenda = await _agendaService.GetDailyAgendaAsync(fecha, cancellationToken);
        return Ok(agenda);
    }

    [HttpPatch("{tomaId:guid}")]
    public async Task<IActionResult> UpdateIntakeStatus(
        [FromRoute] Guid tomaId,
        [FromBody] UpdateMedicationIntakeStatusDto dto,
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

        var result = await _agendaService.UpdateStatusAsync(tomaId, dto, ActorId, cancellationToken);

        return result.Error switch
        {
            null => NoContent(),
            UpdateMedicationIntakeError.NotFound => NotFound(new { message = result.ErrorMessage }),
            UpdateMedicationIntakeError.InvalidStatus => BadRequest(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }
}