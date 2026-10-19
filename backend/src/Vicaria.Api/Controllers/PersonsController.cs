using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vicaria.Api.Filters;
using Vicaria.Application.CasaConvivenciaStays;
using Vicaria.Application.MedicationHistories;
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
    private readonly IMedicationHistoryService _medicationHistoryService;
    private readonly IValidator<GetMedicationHistoryDto> _getMedicationHistoryValidator;

    public PersonsController(
        ISocialRecordService socialRecordService,
        IValidator<UpdatePersonTypeDto> updatePersonTypeValidator,
        IValidator<UpdatePersonProfileStatusDto> updateProfileStatusValidator,
        ICasaConvivenciaStayService casaConvivenciaStayService,
        IProfileTimelineService profileTimelineService,
        IMedicationSchemeService medicationSchemeService,
        IValidator<CreateMedicationSchemeDto> createMedicationSchemeValidator,
        IMedicationHistoryService medicationHistoryService,
        IValidator<GetMedicationHistoryDto> getMedicationHistoryValidator)
    {
        _medicationHistoryService = medicationHistoryService;
        _getMedicationHistoryValidator = getMedicationHistoryValidator;
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
            null when result.Scheme?.StockAlert is not null => Ok(result.Scheme),
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

    // consulta el historial y esquemas vigentes de medicación de la persona (SCRUM-248 / SCRUM-75).
    // Con needsReview=true devuelve solo los vigentes con más de 30 días sin revisión (SCRUM-249).
    [HttpGet("{id}/medication-schemes")]
    [DirectorResidentsOnly]
    [Authorize(Roles = $"{RoleNames.Referent},{RoleNames.CasaConvivenciaDirector},{RoleNames.CasaConvivenciaCoordinator}")]
    public async Task<IActionResult> GetMedicationSchemes(Guid id, [FromQuery] bool? onlyActive, [FromQuery] bool? needsReview, CancellationToken cancellationToken)
    {
        var result = await _medicationSchemeService.GetByPersonIdAsync(id, onlyActive, needsReview, cancellationToken);

        return result.Error switch
        {
            null => Ok(result.Schemes),
            GetPersonMedicationSchemesError.PersonNotFound => NotFound(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }

    // historial de tomas de la persona por fecha, con filtros opcionales from/to/medicationId (SCRUM-240)
    [HttpGet("{id}/medication-history")]
    [DirectorResidentsOnly]
    [Authorize(Roles = $"{RoleNames.Referent},{RoleNames.CasaConvivenciaDirector},{RoleNames.CasaConvivenciaCoordinator}")]
    public async Task<IActionResult> GetMedicationHistory(
        Guid id,
        [FromQuery(Name = "from")] DateOnly? from,
        [FromQuery(Name = "to")] DateOnly? to,
        [FromQuery(Name = "medicationId")] Guid? medicationId,
        CancellationToken cancellationToken)
    {
        var filter = new GetMedicationHistoryDto(from, to, medicationId);

        var validationResult = await _getMedicationHistoryValidator.ValidateAsync(filter, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return ValidationProblem(ModelState);
        }

        var actorRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value);
        var result = await _medicationHistoryService.GetPersonHistoryAsync(id, filter, actorRoles, cancellationToken);

        return result.Error switch
        {
            null => Ok(result.Entries),
            MedicationHistoryError.PersonNotFound => NotFound(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }

    // desactiva un esquema de medicación manteniendo el registro histórico (SCRUM-248 / SCRUM-75)
    [HttpPatch("{id}/medication-schemes/{schemeId:guid}/deactivate")]
    [DirectorResidentsOnly]
    [Authorize(Roles = $"{RoleNames.Referent},{RoleNames.CasaConvivenciaDirector},{RoleNames.CasaConvivenciaCoordinator}")]
    public async Task<IActionResult> DeactivateMedicationScheme(Guid id, Guid schemeId, CancellationToken cancellationToken)
    {
        var result = await _medicationSchemeService.DeactivateAsync(id, schemeId, ActorId, cancellationToken);

        return result.Error switch
        {
            null => NoContent(),
            DeactivateMedicationSchemeError.PersonNotFound => NotFound(new { message = result.ErrorMessage }),
            DeactivateMedicationSchemeError.SchemeNotFound => NotFound(new { message = result.ErrorMessage }),
            DeactivateMedicationSchemeError.AlreadyInactive => Conflict(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }

    // marca el esquema como revisado (SCRUM-249): registra ReviewedAt y ReviewedBy a partir
    // del actor autenticado. Solo esquemas vigentes; el histórico responde 409.
    [HttpPatch("{id}/medication-schemes/{schemeId:guid}/review")]
    [DirectorResidentsOnly]
    [Authorize(Roles = $"{RoleNames.Referent},{RoleNames.CasaConvivenciaDirector},{RoleNames.CasaConvivenciaCoordinator}")]
    public async Task<IActionResult> ReviewMedicationScheme(Guid id, Guid schemeId, CancellationToken cancellationToken)
    {
        var result = await _medicationSchemeService.ReviewAsync(id, schemeId, ActorId, cancellationToken);

        return result.Error switch
        {
            null => NoContent(),
            ReviewMedicationSchemeError.PersonNotFound => NotFound(new { message = result.ErrorMessage }),
            ReviewMedicationSchemeError.SchemeNotFound => NotFound(new { message = result.ErrorMessage }),
            ReviewMedicationSchemeError.AlreadyInactive => Conflict(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }
}