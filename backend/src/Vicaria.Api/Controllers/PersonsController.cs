using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vicaria.Application.CasonaStays;
using Vicaria.Application.Persons;
using Vicaria.Application.SocialRecords;
using Vicaria.Domain.Entities;

namespace Vicaria.Api.Controllers;

[ApiController]
[Route("api/persons")]
[Authorize]
public class PersonsController : ControllerBase
{
    private readonly ISocialRecordService _socialRecordService;
    private readonly IValidator<UpdatePersonTypeDto> _updatePersonTypeValidator;
    private readonly IValidator<UpdatePersonProfileStatusDto> _updateProfileStatusValidator;
    private readonly ICasonaStayService _casonaStayService;

    public PersonsController(
        ISocialRecordService socialRecordService,
        IValidator<UpdatePersonTypeDto> updatePersonTypeValidator,
        IValidator<UpdatePersonProfileStatusDto> updateProfileStatusValidator,
        ICasonaStayService casonaStayService)
    {
        _socialRecordService = socialRecordService;
        _updatePersonTypeValidator = updatePersonTypeValidator;
        _updateProfileStatusValidator = updateProfileStatusValidator;
        _casonaStayService = casonaStayService;
    }

    private Guid ActorId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("{id}/casona-stays")]
    public async Task<ActionResult<IEnumerable<CasonaStayDto>>> GetCasonaStays(Guid id, CancellationToken cancellationToken)
    {
        var stays = await _casonaStayService.GetByPersonIdAsync(id, cancellationToken);
        return Ok(stays);
    }

    // cambia el tipo de persona (SCRUM-134): para Residente exige evaluación
    // psiquiátrica vigente; sin ella responde 400 con el detalle
    [HttpPut("{id}/type")]
    [Authorize(Roles = $"{RoleNames.Referente},{RoleNames.DirectoraDeCasona},{RoleNames.CoordinadorDeCasaConvivencia}")]
    public async Task<IActionResult> UpdateType(Guid id, [FromBody] UpdatePersonTypeDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await _updatePersonTypeValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return ValidationProblem(ModelState);
        }

        var result = await _socialRecordService.UpdatePersonTypeAsync(id, dto, ActorId, cancellationToken);

        return result.Error switch
        {
            null => NoContent(),
            UpdatePersonTypeError.PersonNotFound => NotFound(new { message = result.ErrorMessage }),
            UpdatePersonTypeError.SocialRecordNotFound => NotFound(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }
    // cambia el estado del perfil (ambulatorio activo/inactivo, residente) y audita el cambio (SCRUM-152/153)
    [HttpPut("{id}/status")]
    [Authorize(Roles = $"{RoleNames.Referente},{RoleNames.DirectoraDeCasona},{RoleNames.CoordinadorDeCasaConvivencia}")]
    public async Task<IActionResult> UpdateProfileStatus(Guid id, [FromBody] UpdatePersonProfileStatusDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await _updateProfileStatusValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors) { ModelState.AddModelError(error.PropertyName, error.ErrorMessage); }
            return ValidationProblem(ModelState);
        }

        var result = await _socialRecordService.UpdatePersonProfileStatusAsync(id, dto, ActorId, cancellationToken);

        return result.Error switch
        {
            null => NoContent(),
            UpdatePersonProfileStatusError.PersonNotFound => NotFound(new { message = result.ErrorMessage }),
            UpdatePersonProfileStatusError.SocialRecordNotFound => NotFound(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }
}