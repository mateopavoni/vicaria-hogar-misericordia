using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    private readonly IValidator<UpdatePersonStatusDto> _updatePersonStatusValidator;

    public PersonsController(
        ISocialRecordService socialRecordService,
        IValidator<UpdatePersonTypeDto> updatePersonTypeValidator,
        IValidator<UpdatePersonStatusDto> updatePersonStatusValidator)
    {
        _socialRecordService = socialRecordService;
        _updatePersonTypeValidator = updatePersonTypeValidator;
        _updatePersonStatusValidator = updatePersonStatusValidator;
    }

    private Guid ActorId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

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
    // cambia el estado de la persona y audita el cambio (SCRUM-136)
    [HttpPut("{id}/status")]
    [Authorize(Roles = $"{RoleNames.Referente},{RoleNames.DirectoraDeCasona},{RoleNames.CoordinadorDeCasaConvivencia}")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdatePersonStatusDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await _updatePersonStatusValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return ValidationProblem(ModelState);
        }

        var result = await _socialRecordService.UpdateStatusAsync(id, dto, ActorId, cancellationToken);

        return result.Error switch
        {
            null => NoContent(),
            UpdatePersonStatusError.PersonNotFound => NotFound(new { message = result.ErrorMessage }),
            UpdatePersonStatusError.SocialRecordNotFound => NotFound(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }
}