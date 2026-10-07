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

    public CollaboratorsController(
        ICollaboratorService collaboratorService,
        IValidator<CreateCollaboratorDto> createValidator)
    {
        _collaboratorService = collaboratorService;
        _createValidator = createValidator;
    }

    private Guid ActorId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

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
}
