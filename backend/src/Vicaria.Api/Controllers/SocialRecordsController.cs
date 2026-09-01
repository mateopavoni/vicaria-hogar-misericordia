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
    private readonly IValidator<UpdateSocialRecordDto> _updateValidator;

    public SocialRecordsController(
        ISocialRecordService socialRecordService,
        IValidator<CreateSocialRecordDto> createValidator,
        IValidator<UpdateSocialRecordDto> updateValidator)
    {
        _socialRecordService = socialRecordService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
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

    // cualquier rol autenticado puede buscar (SCRUM-6), incluida Escucha
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? q, CancellationToken cancellationToken)
    {
        var results = await _socialRecordService.SearchAsync(q, cancellationToken);
        return Ok(results);
    }

    // solo Referente y Directora pueden editar (SCRUM-7)
    [HttpPut("{id}")]
    [Authorize(Roles = $"{RoleNames.Referente},{RoleNames.DirectoraDeCasona}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSocialRecordDto dto, CancellationToken cancellationToken)
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

        var result = await _socialRecordService.UpdateAsync(id, dto, ActorId, cancellationToken);
        return result.Success ? NoContent() : NotFound(new { message = result.ErrorMessage });
    }
}
