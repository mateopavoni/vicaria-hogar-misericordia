using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vicaria.Application.SocialRecords;
using Vicaria.Domain.Entities;

namespace Vicaria.Api.Controllers;

[ApiController]
[Route("api/social-records")]
[Authorize]
public class SocialRecordsController : ControllerBase
{
    private readonly ISocialRecordService _socialRecordService;
    private readonly IValidator<CreateSocialRecordDto> _createValidator;

    public SocialRecordsController(ISocialRecordService socialRecordService, IValidator<CreateSocialRecordDto> createValidator)
    {
        _socialRecordService = socialRecordService;
        _createValidator = createValidator;
    }

    private Guid ActorId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Escucha no puede crear fichas (SCRUM-5), solo verlas y cargar observaciones
    [HttpPost]
    [Authorize(Roles = $"{RoleNames.Referente},{RoleNames.DirectoraDeCasona},{RoleNames.CoordinadorDeCasaConvivencia}")]
    public async Task<IActionResult> Create([FromBody] CreateSocialRecordDto dto, CancellationToken cancellationToken)
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

        var result = await _socialRecordService.CreateAsync(dto, ActorId, cancellationToken);
        return CreatedAtAction(nameof(Create), new { id = result.SocialRecordId }, new { personId = result.PersonId, id = result.SocialRecordId });
    }
}
