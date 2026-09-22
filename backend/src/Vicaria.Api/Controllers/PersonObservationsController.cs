using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vicaria.Application.Observations;
using Vicaria.Domain.Entities;

namespace Vicaria.Api.Controllers;

[ApiController]
[Route("api/persons/{id:guid}/observations")]
public class PersonObservationsController : ControllerBase
{
    private readonly IObservationService _observationService;
    private readonly IValidator<CreateObservationDto> _validator;

    public PersonObservationsController(
        IObservationService observationService,
        IValidator<CreateObservationDto> validator)
    {
        _observationService = observationService;
        _validator = validator;
    }

    private Guid ActorId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    [Authorize(Roles = $"{RoleNames.Referente},{RoleNames.DirectoraDeCasona},{RoleNames.Escucha}")]
    public async Task<IActionResult> Create(
        [FromRoute] Guid id,
        [FromBody] CreateObservationDto dto,
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

        var result = await _observationService.CreateObservationAsync(id, dto, ActorId, cancellationToken);

        if (!result.Success)
        {
            return result.Error switch
            {
                CreateObservationError.PersonNotFound => NotFound(new { message = result.ErrorMessage }),
                CreateObservationError.CategoryNotFoundOrInactive => BadRequest(new { message = result.ErrorMessage }),
                _ => BadRequest(new { message = result.ErrorMessage })
            };
        }

        return StatusCode(StatusCodes.Status201Created, result.Data);
    }
}