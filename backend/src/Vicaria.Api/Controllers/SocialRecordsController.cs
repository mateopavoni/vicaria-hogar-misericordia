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
    // SCRUM-137: Directora de Casa de Convivencia solo recibe Residentes; Referente ve todos.
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? q, CancellationToken cancellationToken)
    {
        PersonType? personTypeFilter = null;

        if (User.IsInRole(RoleNames.DirectoraDeCasona))
        {
            personTypeFilter = PersonType.Resident;
        }

        var results = await _socialRecordService.SearchAsync(q, personTypeFilter, cancellationToken);
        return Ok(results);
    }

    // listado paginado con busqueda y filtros combinables para la pantalla de fichas (SCRUM-6/21/127/164)
    [HttpGet("list")]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? entryDateFrom = null,
        [FromQuery] DateTime? entryDateTo = null,
        [FromQuery] int? withoutObservationsDays = null,
        [FromQuery] bool? hasDni = null,
        [FromQuery] bool? hasAddress = null,
        [FromQuery] SocialRecordStatus? status = null,
        [FromQuery] PersonType? personType = null,
        CancellationToken cancellationToken = default)
    {
        PersonType? personTypeFilter = User.IsInRole(RoleNames.DirectoraDeCasona) ? PersonType.Resident : null;
        var filter = new FilterSocialRecordsDto(entryDateFrom, entryDateTo, withoutObservationsDays, hasDni, hasAddress, status, personType);

        var result = await _socialRecordService.GetPagedAsync(page, search, filter, personTypeFilter, cancellationToken);
        return Ok(result);
    }

    // perfil completo de una ficha (SCRUM-8/121)
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var record = await _socialRecordService.GetByIdAsync(id, cancellationToken);
        return record is null ? NotFound() : Ok(record);
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
    [HttpGet("filter/count")]
    public async Task<IActionResult> CountByFilter([FromQuery] FilterSocialRecordsDto filter, CancellationToken cancellationToken)
    {
        var count = await _socialRecordService.CountByFilterAsync(filter, cancellationToken);
        return Ok(new { count });
    }
}
