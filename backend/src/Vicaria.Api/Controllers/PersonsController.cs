using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vicaria.Api.Filters;
using Vicaria.Application.CasaConvivenciaStays;
using Vicaria.Application.MedicationSchemes;
using Vicaria.Application.Persons;
using Vicaria.Application.SocialRecords;
using Vicaria.Application.Timelines;
using Vicaria.Domain.Entities;

namespace Vicaria.Api.Controllers;

[ApiController]
[Route("api/persons")]
[Authorize]
public class PersonsController : ControllerBase
{
    private readonly ISocialRecordService _socialRecordService;
    private readonly IValidator<UpdatePersonTypeDto> _updatePersonTypeValidator;
    private readonly IValidator<UpdatePersonProfileStatusDto> _updateProfileStatusValidator;
    private readonly ICasaConvivenciaStayService _casaConvivenciaStayService;
    private readonly IProfileTimelineService _profileTimelineService;
    private readonly IMedicationSchemeService _medicationSchemeService;
    private readonly IValidator<CreateMedicationSchemeDto> _createMedicationSchemeValidator;

    public PersonsController(
        ISocialRecordService socialRecordService,
        IValidator<UpdatePersonTypeDto> updatePersonTypeValidator,
        IValidator<UpdatePersonProfileStatusDto> updateProfileStatusValidator,
        ICasaConvivenciaStayService casaConvivenciaStayService,
        IProfileTimelineService profileTimelineService,
        IMedicationSchemeService medicationSchemeService,
        IValidator<CreateMedicationSchemeDto> createMedicationSchemeValidator)
    {
        _socialRecordService = socialRecordService;
        _updatePersonTypeValidator = updatePersonTypeValidator;
        _updateProfileStatusValidator = updateProfileStatusValidator;
        _casaConvivenciaStayService = casaConvivenciaStayService;
        _profileTimelineService = profileTimelineService;
        _medicationSchemeService = medicationSchemeService;
        _createMedicationSchemeValidator = createMedicationSchemeValidator;
    }

    private Guid ActorId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("{id}/casa-convivencia-stays")]
    [DirectorResidentsOnly]
    public async Task<ActionResult<IEnumerable<CasaConvivenciaStayDto>>> GetCasaConvivenciaStays(Guid id, CancellationToken cancellationToken)
    {
        var stays = await _casaConvivenciaStayService.GetByPersonIdAsync(id, cancellationToken);
        return Ok(stays);
    }

    // cambia el tipo de persona (SCRUM-134): para Residente exige evaluación
    // psiquiátrica vigente; sin ella responde 400 con el detalle
    [HttpPut("{id}/type")]
    [DirectorResidentsOnly]
    [Authorize(Roles = $"{RoleNames.Referent},{RoleNames.CasaConvivenciaDirector},{RoleNames.CasaConvivenciaCoordinator}")]
    public async Task<IActionResult> UpdateType(Guid id, [FromBody] UpdatePersonTypeDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await _updatePersonTypeValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return ValidationProblem(ModelState);
        }

        var result = await _socialRecordService.UpdatePersonTypeAsync(id, dto, ActorId, cancellationToken);

        return result.Error switch
        {
            null => NoContent(),
            UpdatePersonTypeError.PersonNotFound => NotFound(new { message = result.ErrorMessage }),
            UpdatePersonTypeError.SocialRecordNotFound => NotFound(new { message = result.ErrorMessage }),
            UpdatePersonTypeError.ActiveStayMustBeExitedFirst => Conflict(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }
    // cambia el estado del perfil (ambulatorio activo/inactivo, residente) y audita el cambio (SCRUM-152/153)
    [HttpPut("{id}/status")]
    [DirectorResidentsOnly]
    [Authorize(Roles = $"{RoleNames.Referent},{RoleNames.CasaConvivenciaDirector},{RoleNames.CasaConvivenciaCoordinator}")]
    public async Task<IActionResult> UpdateProfileStatus(Guid id, [FromBody] UpdatePersonProfileStatusDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await _updateProfileStatusValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors) { ModelState.AddModelError(error.PropertyName, error.ErrorMessage); }
            return ValidationProblem(ModelState);
        }

        var result = await _socialRecordService.UpdatePersonProfileStatusAsync(id, dto, ActorId, cancellationToken);

        return result.Error switch
        {
            null => NoContent(),
            UpdatePersonProfileStatusError.PersonNotFound => NotFound(new { message = result.ErrorMessage }),
            UpdatePersonProfileStatusError.SocialRecordNotFound => NotFound(new { message = result.ErrorMessage }),
            UpdatePersonProfileStatusError.ActiveStayMustBeExitedFirst => Conflict(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }
    // carga de esquema de medicación (SCRUM-232 / SCRUM-59): medicamento del
    // catálogo + dosis + hora (HH:mm). Solo Residentes con evaluación
    // psiquiátrica vigente; el duplicado vigente responde 409. Devuelve el
    // esquema completo como lo usa el front (SCRUM-234).
    [HttpPost("{id}/medication-schemes")]
    [DirectorResidentsOnly]
    [Authorize(Roles = $"{RoleNames.Referent},{RoleNames.CasaConvivenciaDirector},{RoleNames.CasaConvivenciaCoordinator}")]
    public async Task<IActionResult> CreateMedicationScheme(Guid id, [FromBody] CreateMedicationSchemeDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await _createMedicationSchemeValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return ValidationProblem(ModelState);
        }

        var result = await _medicationSchemeService.CreateAsync(id, dto, ActorId, cancellationToken);

        return result.Error switch
        {
            null => StatusCode(StatusCodes.Status201Created, result.Scheme),
            CreateMedicationSchemeError.PersonNotFound => NotFound(new { message = result.ErrorMessage }),
            CreateMedicationSchemeError.MedicationNotFound => NotFound(new { message = result.ErrorMessage }),
            CreateMedicationSchemeError.DuplicateScheme => Conflict(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }
    // timeline unificado del perfil (SCRUM-159): hitos del expediente (estadías de
    // Casa de Convivencia) + observaciones, ordenados por fecha. Lectura para los roles con acceso al perfil.
    [HttpGet("{id}/timeline")]
    [DirectorResidentsOnly]
    [Authorize(Roles = $"{RoleNames.Referent},{RoleNames.CasaConvivenciaDirector},{RoleNames.Listener}")]
    public async Task<IActionResult> GetTimeline(Guid id, CancellationToken cancellationToken)
    {
        var result = await _profileTimelineService.GetTimelineAsync(id, cancellationToken);

        return result.Error switch
        {
            null => Ok(result.Data),
            ProfileTimelineError.PersonNotFound => NotFound(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }
}