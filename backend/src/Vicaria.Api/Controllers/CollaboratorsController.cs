using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vicaria.Application.Collaborators;
using Vicaria.Domain.Entities;

namespace Vicaria.Api.Controllers;

[ApiController]
[Route("api/collaborators")]
[Authorize]
public class CollaboratorsController : ControllerBase
{
    private readonly ICollaboratorService _collaboratorService;
    private readonly IValidator<CreateCollaboratorDto> _createValidator;
    private readonly IValidator<SearchCollaboratorsDto> _searchValidator;
    private readonly IValidator<UpdateCollaboratorDto> _updateValidator;

    public CollaboratorsController(
        ICollaboratorService collaboratorService,
        IValidator<CreateCollaboratorDto> createValidator,
        IValidator<SearchCollaboratorsDto> searchValidator,
        IValidator<UpdateCollaboratorDto> updateValidator)
    {
        _collaboratorService = collaboratorService;
        _createValidator = createValidator;
        _searchValidator = searchValidator;
        _updateValidator = updateValidator;
    }

    private Guid ActorId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // listado completo para la pantalla de gestión; mismo criterio de acceso que la búsqueda
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var results = await _collaboratorService.ListAsync(cancellationToken);
        return Ok(results);
    }

    // búsqueda por nombre, apellido o área, con filtro opcional por tipo (SCRUM-204).
    // cualquier rol autenticado puede buscar, mismo criterio que la búsqueda de fichas
    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] SearchCollaboratorsDto dto,
        CancellationToken cancellationToken)
    {
        var validationResult = await _searchValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return ValidationProblem(ModelState);
        }

        var results = await _collaboratorService.SearchAsync(dto.Q, dto.Type, cancellationToken);
        return Ok(results);
    }

    // alta solo para Referente (SCRUM-199)
    [HttpPost]
    [Authorize(Roles = RoleNames.Referent)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCollaboratorDto dto,
        CancellationToken cancellationToken)
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

        var result = await _collaboratorService.CreateAsync(dto, ActorId, cancellationToken);
        return result.Error switch
        {
            null => StatusCode(StatusCodes.Status201Created, new { id = result.CollaboratorId }),
            CreateCollaboratorError.DuplicateDni => Conflict(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = RoleNames.Referent)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateCollaboratorDto dto,
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

        var result = await _collaboratorService.UpdateAsync(id, dto, ActorId, cancellationToken);
        return result.Error switch
        {
            null => NoContent(),
            UpdateCollaboratorError.NotFound => NotFound(new { message = result.ErrorMessage }),
            UpdateCollaboratorError.DuplicateDni => Conflict(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _collaboratorService.GetByIdAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(new { message = result.ErrorMessage });
        }

        return Ok(result.Collaborator);
    }
}
