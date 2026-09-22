using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vicaria.Application.ObservationCategories;
using Vicaria.Domain.Entities;

namespace Vicaria.Api.Controllers;

[ApiController]
[Route("api/observation-categories")]
[Authorize]
public class ObservationCategoryController : ControllerBase
{
    private readonly IObservationCategoryService _observationCategoryService;
    private readonly IValidator<CreateObservationCategoryDto> _createValidator;
    private readonly IValidator<UpdateObservationCategoryDto> _updateValidator;

    public ObservationCategoryController(
        IObservationCategoryService observationCategoryService,
        IValidator<CreateObservationCategoryDto> createValidator,
        IValidator<UpdateObservationCategoryDto> updateValidator)
    {
        _observationCategoryService = observationCategoryService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    private Guid ActorId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // solo Directora y Coordinador gestionan categorías (SCRUM-176); la Escucha solo las usa al cargar observaciones
    [HttpPost]
    [Authorize(Roles = $"{RoleNames.DirectoraDeCasona},{RoleNames.CoordinadorDeCasaConvivencia}")]
    public async Task<IActionResult> Create([FromBody] CreateObservationCategoryDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await _createValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return ValidationProblem(ModelState);
        }

        var result = await _observationCategoryService.CreateAsync(dto, ActorId, cancellationToken);
        return CreatedAtAction(nameof(Create), new { id = result.ObservationCategoryId }, new { id = result.ObservationCategoryId });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = $"{RoleNames.DirectoraDeCasona},{RoleNames.CoordinadorDeCasaConvivencia}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateObservationCategoryDto dto, CancellationToken cancellationToken)
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

        var result = await _observationCategoryService.UpdateAsync(id, dto, ActorId, cancellationToken);
        return result.Success ? NoContent() : NotFound(new { message = result.ErrorMessage });
    }

    // desactivación (soft delete, SCRUM-176): nunca hay borrado físico
    [HttpPatch("{id}/deactivate")]
    [Authorize(Roles = $"{RoleNames.DirectoraDeCasona},{RoleNames.CoordinadorDeCasaConvivencia}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _observationCategoryService.DeactivateAsync(id, ActorId, cancellationToken);

        return result.Error switch
        {
            null => NoContent(),
            DeactivateObservationCategoryError.CategoryNotFound => NotFound(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }
}