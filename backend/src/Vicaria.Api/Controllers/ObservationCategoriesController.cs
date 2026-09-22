using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vicaria.Application.Observations;
using Vicaria.Domain.Entities;

namespace Vicaria.Api.Controllers;

[ApiController]
[Route("api/observation-categories")]
public class ObservationCategoriesController : ControllerBase
{
    private readonly IObservationCategoryService _categoryService;
    private readonly IValidator<CreateObservationCategoryDto> _createValidator;
    private readonly IValidator<UpdateObservationCategoryDto> _updateValidator;

    public ObservationCategoriesController(IObservationCategoryService categoryService, IValidator<CreateObservationCategoryDto> createValidator, IValidator<UpdateObservationCategoryDto> updateValidator)
    {
        _categoryService = categoryService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    [Authorize(Roles = $"{RoleNames.Referente},{RoleNames.DirectoraDeCasona},{RoleNames.Escucha}")]
    public async Task<IActionResult> GetAll([FromQuery] bool onlyActive = true, CancellationToken cancellationToken = default) =>
        Ok(await _categoryService.GetCategoriesAsync(onlyActive, cancellationToken));

    [HttpPost]
    [Authorize(Roles = RoleNames.Referente)]
    public async Task<IActionResult> Create([FromBody] CreateObservationCategoryDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await _createValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors) ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            return ValidationProblem(ModelState);
        }

        var result = await _categoryService.CreateAsync(dto, cancellationToken);
        if (!result.Success) return Conflict(new { message = result.ErrorMessage });
        return StatusCode(StatusCodes.Status201Created, result.Data);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = RoleNames.Referente)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateObservationCategoryDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await _updateValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors) ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            return ValidationProblem(ModelState);
        }

        var result = await _categoryService.UpdateAsync(id, dto, cancellationToken);
        return result.Error switch
        {
            null => Ok(result.Data),
            CategoryOperationError.NotFound => NotFound(new { message = result.ErrorMessage }),
            CategoryOperationError.DuplicateName => Conflict(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = RoleNames.Referente)]
    public async Task<IActionResult> ToggleStatus(Guid id, [FromBody] ToggleObservationCategoryStatusDto dto, CancellationToken cancellationToken)
    {
        var result = await _categoryService.ToggleStatusAsync(id, dto.IsActive, cancellationToken);
        if (!result.Success) return NotFound(new { message = result.ErrorMessage });
        return NoContent();
    }
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = RoleNames.Referente)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _categoryService.DeleteAsync(id, cancellationToken);
        return result.Error switch
        {
            null => NoContent(),
            CategoryOperationError.NotFound => NotFound(new { message = result.ErrorMessage }),
            CategoryOperationError.HasAssociatedObservations => Conflict(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }
}