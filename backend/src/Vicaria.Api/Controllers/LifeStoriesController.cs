using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vicaria.Api.Filters;
using Vicaria.Application.Common;
using Vicaria.Application.LifeStories;
using Vicaria.Domain.Entities;

namespace Vicaria.Api.Controllers;

[ApiController]
[Route("api/persons/{personId:guid}/life-story")]
[DirectorResidentsOnly("personId")]
[Authorize]
public class LifeStoriesController : ControllerBase
{
    private readonly ILifeStoryService _lifeStoryService;
    private readonly IValidator<UpdateLifeStoryStageDto> _updateStageValidator;

    public LifeStoriesController(ILifeStoryService lifeStoryService, IValidator<UpdateLifeStoryStageDto> updateStageValidator)
    {
        _lifeStoryService = lifeStoryService;
        _updateStageValidator = updateStageValidator;
    }

    private Guid ActorId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [Authorize(Roles = $"{RoleNames.Referent},{RoleNames.CasaConvivenciaDirector},{RoleNames.Listener}")]
    public async Task<ActionResult<LifeStoryResponseDto>> Get(Guid personId, CancellationToken cancellationToken)
    {
        var result = await _lifeStoryService.GetByPersonIdAsync(personId, cancellationToken);
        return Ok(result);
    }

    [HttpPut]
    [Authorize(Roles = RoleNames.Referent)]
    public async Task<ActionResult<LifeStoryResponseDto>> Update(
        Guid personId,
        [FromBody] UpdateLifeStoryDto dto,
        CancellationToken cancellationToken)
    {
        if (new[] { dto.BeforeCentroBarrial, dto.InCentroBarrial, dto.AfterCentroBarrial }.Any(SensitiveContentRules.ContainsSensitiveContent))
        {
            ModelState.AddModelError("content", SensitiveContentRules.Message);
            return ValidationProblem(ModelState);
        }

        var result = await _lifeStoryService.UpdateAsync(personId, dto, ActorId, cancellationToken);
        if (result is null) return NotFound(new { message = "La persona especificada no existe." });

        return Ok(result);
    }

    // edición de una sola etapa, de forma independiente (SCRUM-171): cada etapa se
    // edita por separado y en momentos distintos; autor y fecha quedan por etapa
    [HttpPut("{stage}")]
    [Authorize(Roles = RoleNames.Referent)]
    public async Task<ActionResult<LifeStoryResponseDto>> UpdateStage(
        Guid personId,
        string stage,
        [FromBody] UpdateLifeStoryStageDto dto,
        CancellationToken cancellationToken)
    {
        var validationResult = await _updateStageValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors) ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            return ValidationProblem(ModelState);
        }

        var stageEnum = ParseStage(stage);
        if (stageEnum is null)
        {
            ModelState.AddModelError("stage", "La etapa no es válida. Valores admitidos: before-centro-barrial, in-centro-barrial, after-centro-barrial.");
            return ValidationProblem(ModelState);
        }

        var result = await _lifeStoryService.UpdateStageAsync(personId, stageEnum.Value, dto.Content, ActorId, cancellationToken);
        if (result is null) return NotFound(new { message = "La persona especificada no existe." });

        return Ok(result);
    }

    private static LifeStoryStage? ParseStage(string stage) => stage switch
    {
        "before-centro-barrial" => LifeStoryStage.BeforeCentroBarrial,
        "in-centro-barrial" => LifeStoryStage.InCentroBarrial,
        "after-centro-barrial" => LifeStoryStage.AfterCentroBarrial,
        _ => null
    };
}