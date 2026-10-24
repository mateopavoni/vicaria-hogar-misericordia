using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vicaria.Application.CaritasCatalogs;
using Vicaria.Domain.Entities;

namespace Vicaria.Api.Controllers;

[ApiController]
[Route("api/caritas-catalogs")]
public class CaritasCatalogsController : ControllerBase
{
    private readonly ICaritasCatalogService _catalogService;
    private readonly IValidator<CreateCaritasCatalogValueDto> _createValidator;
    private readonly IValidator<UpdateCaritasCatalogValueDto> _updateValidator;

    public CaritasCatalogsController(ICaritasCatalogService catalogService, IValidator<CreateCaritasCatalogValueDto> createValidator, IValidator<UpdateCaritasCatalogValueDto> updateValidator)
    {
        _catalogService = catalogService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    private Guid ActorId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [Authorize(Roles = $"{RoleNames.Referent},{RoleNames.CasaConvivenciaDirector},{RoleNames.Listener},{RoleNames.CasaConvivenciaCoordinator}")]
    public async Task<IActionResult> GetAll([FromQuery] CaritasCatalogCategory? category, [FromQuery] bool onlyActive = true, CancellationToken cancellationToken = default) =>
        Ok(await _catalogService.GetValuesAsync(category, onlyActive, cancellationToken));

    [HttpPost]
    [Authorize(Roles = RoleNames.Referent)]
    public async Task<IActionResult> Create([FromBody] CreateCaritasCatalogValueDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await _createValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors) ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            return ValidationProblem(ModelState);
        }

        var result = await _catalogService.CreateAsync(dto, ActorId, cancellationToken);
        if (!result.Success) return Conflict(new { message = result.ErrorMessage });
        return StatusCode(StatusCodes.Status201Created, result.Data);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = RoleNames.Referent)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCaritasCatalogValueDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await _updateValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors) ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            return ValidationProblem(ModelState);
        }

        var result = await _catalogService.UpdateAsync(id, dto, ActorId, cancellationToken);
        return result.Error switch
        {
            null => Ok(result.Data),
            CaritasCatalogOperationError.NotFound => NotFound(new { message = result.ErrorMessage }),
            CaritasCatalogOperationError.DuplicateName => Conflict(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = RoleNames.Referent)]
    public async Task<IActionResult> ToggleStatus(Guid id, [FromBody] ToggleCaritasCatalogValueStatusDto dto, CancellationToken cancellationToken)
    {
        var result = await _catalogService.ToggleStatusAsync(id, dto.IsActive, ActorId, cancellationToken);
        if (!result.Success) return NotFound(new { message = result.ErrorMessage });
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = RoleNames.Referent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _catalogService.DeleteAsync(id, ActorId, cancellationToken);
        if (!result.Success) return NotFound(new { message = result.ErrorMessage });
        return NoContent();
    }
}
