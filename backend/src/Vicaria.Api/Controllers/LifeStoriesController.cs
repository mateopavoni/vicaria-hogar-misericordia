using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vicaria.Application.LifeStories;
using Vicaria.Domain.Entities;

namespace Vicaria.Api.Controllers;

[ApiController]
[Route("api/persons/{personId:guid}/life-story")]
[Authorize]
public class LifeStoriesController : ControllerBase
{
    private readonly ILifeStoryService _lifeStoryService;

    public LifeStoriesController(ILifeStoryService lifeStoryService)
    {
        _lifeStoryService = lifeStoryService;
    }

    private Guid ActorId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [Authorize(Roles = $"{RoleNames.Referente},{RoleNames.DirectoraDeCasona},{RoleNames.Escucha}")]
    public async Task<ActionResult<LifeStoryResponseDto>> Get(Guid personId, CancellationToken cancellationToken)
    {
        var result = await _lifeStoryService.GetByPersonIdAsync(personId, cancellationToken);
        return Ok(result);
    }

    [HttpPut]
    [Authorize(Roles = RoleNames.Referente)]
    public async Task<ActionResult<LifeStoryResponseDto>> Update(
        Guid personId,
        [FromBody] UpdateLifeStoryDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _lifeStoryService.UpdateAsync(personId, dto, ActorId, cancellationToken);
        if (result is null) return NotFound(new { message = "La persona especificada no existe." });

        return Ok(result);
    }
}