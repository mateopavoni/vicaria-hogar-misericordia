using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vicaria.Api.Filters;
using Vicaria.Application.CasaConvivenciaStays;
using Vicaria.Application.MedicationHistories;
using Vicaria.Application.MedicationSchemes;
using Vicaria.Application.PersonExports;
using Vicaria.Application.PersonAttachments;
using Vicaria.Application.Persons;
using Vicaria.Application.PsychiatricEvaluations;
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
    private readonly IValidator<UpdatePersonStatusDto> _updatePersonStatusValidator;
    private readonly ICasaConvivenciaStayService _casaConvivenciaStayService;
    private readonly IProfileTimelineService _profileTimelineService;
    private readonly IMedicationSchemeService _medicationSchemeService;
    private readonly IValidator<CreateMedicationSchemeDto> _createMedicationSchemeValidator;
    private readonly IMedicationHistoryService _medicationHistoryService;
    private readonly IMedicationHistoryPdfGenerator _medicationHistoryPdfGenerator;
    private readonly IValidator<GetMedicationHistoryDto> _getMedicationHistoryValidator;
    private readonly IPsychiatricEvaluationService _psychiatricEvaluationService;
    private readonly IValidator<CreatePsychiatricEvaluationDto> _createPsychiatricEvaluationValidator;
    private readonly IPersonAttachmentService _personAttachmentService;
    private readonly IPersonExportService _personExportService;
    private readonly IPersonExportPdfGenerator _personExportPdfGenerator;
    

    public PersonsController(
        ISocialRecordService socialRecordService,
        IValidator<UpdatePersonTypeDto> updatePersonTypeValidator,
        IValidator<UpdatePersonProfileStatusDto> updateProfileStatusValidator,
        IValidator<UpdatePersonStatusDto> updatePersonStatusValidator,
        ICasaConvivenciaStayService casaConvivenciaStayService,
        IProfileTimelineService profileTimelineService,
        IMedicationSchemeService medicationSchemeService,
        IValidator<CreateMedicationSchemeDto> createMedicationSchemeValidator,
        IMedicationHistoryService medicationHistoryService,
        IMedicationHistoryPdfGenerator medicationHistoryPdfGenerator,
        IValidator<GetMedicationHistoryDto> getMedicationHistoryValidator,
        IPsychiatricEvaluationService psychiatricEvaluationService,
        IValidator<CreatePsychiatricEvaluationDto> createPsychiatricEvaluationValidator,
        IPersonAttachmentService personAttachmentService,
        IPersonExportService personExportService,
        IPersonExportPdfGenerator personExportPdfGenerator)
       
    {
        _socialRecordService = socialRecordService;
        _updatePersonTypeValidator = updatePersonTypeValidator;
        _updateProfileStatusValidator = updateProfileStatusValidator;
        _updatePersonStatusValidator = updatePersonStatusValidator;
        _casaConvivenciaStayService = casaConvivenciaStayService;
        _profileTimelineService = profileTimelineService;
        _medicationSchemeService = medicationSchemeService;
        _createMedicationSchemeValidator = createMedicationSchemeValidator;
        _medicationHistoryService = medicationHistoryService;
        _medicationHistoryPdfGenerator = medicationHistoryPdfGenerator;
        _getMedicationHistoryValidator = getMedicationHistoryValidator;
        _psychiatricEvaluationService = psychiatricEvaluationService;
        _createPsychiatricEvaluationValidator = createPsychiatricEvaluationValidator;
        _personAttachmentService = personAttachmentService;
        _personExportService = personExportService;
        _personExportPdfGenerator = personExportPdfGenerator;
        
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
            CreateMedicationSchemeError.MissingPsychiatricEvaluation => Conflict(new { message = result.ErrorMessage }),
            CreateMedicationSchemeError.DuplicateScheme => Conflict(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }
    // alta de evaluación psiquiátrica con unicidad de vigente (SCRUM-258): invalida las
    // anteriores de la persona, actualiza HasPsychiatricEvaluation en la ficha y calcula
    // el vencimiento (fecha + 6 meses). Solo Referente, Directora y Coordinador; Escucha → 403.
    [HttpPost("{id}/psychiatric-evaluations")]
    [DirectorResidentsOnly]
    [Authorize(Roles = $"{RoleNames.Referent},{RoleNames.CasaConvivenciaDirector},{RoleNames.CasaConvivenciaCoordinator}")]
    public async Task<IActionResult> CreatePsychiatricEvaluation(Guid id, [FromBody] CreatePsychiatricEvaluationDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await _createPsychiatricEvaluationValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return ValidationProblem(ModelState);
        }

        var result = await _psychiatricEvaluationService.CreateAsync(id, dto, ActorId, cancellationToken);

        return result.Error switch
        {
            null => StatusCode(StatusCodes.Status201Created, result.Evaluation),
            CreatePsychiatricEvaluationError.PersonNotFound => NotFound(new { message = result.ErrorMessage }),
            CreatePsychiatricEvaluationError.SocialRecordNotFound => NotFound(new { message = result.ErrorMessage }),
            CreatePsychiatricEvaluationError.DuplicateValidEvaluation => Conflict(new { message = result.ErrorMessage }),
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

    // exportación de la ficha a PDF (SCRUM-252): secciones datos /
    // datos-observaciones / todo, con nombre del sistema, fecha y referente;
    // excluye autores de observaciones y registra la exportación en auditoría
    // solo el Referente exporta, igual que POST social-records/{id}/exports
    [HttpGet("{id:guid}/export-pdf")]
    [Authorize(Roles = RoleNames.Referent)]
    public async Task<IActionResult> ExportPdf(
        Guid id,
        [FromQuery(Name = "sections")] string? sections,
        CancellationToken cancellationToken)
    {
        var result = await _personExportService.GetExportAsync(id, sections ?? string.Empty, ActorId, cancellationToken);

        return result.Error switch
        {
            null => File(
                _personExportPdfGenerator.GenerateExportPdf(result.Document!),
                "application/pdf",
                BuildPersonExportFileName(result.Document!)),
            PersonExportError.PersonNotFound => NotFound(new { message = result.ErrorMessage }),
            PersonExportError.SocialRecordNotFound => NotFound(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }

    private static string BuildPersonExportFileName(PersonExportDocument document)
    {
        var baseName = string.Concat(
            document.FirstName.Trim().ToLowerInvariant()
                .Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-')
                .Select(c => c == ' ' ? '-' : c));
        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "persona";
        }

        return $"ficha-{baseName.ToLowerInvariant()}-{document.GeneratedAtUtc:yyyyMMdd}.pdf";
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

    // exportación del historial a PDF para presentar a un profesional de salud (SCRUM-241):
    // mismos filtros, misma autorización y mismo conjunto que el JSON (SCRUM-240)
    [HttpGet("{id}/medication-history/pdf")]
    [DirectorResidentsOnly]
    [Authorize(Roles = $"{RoleNames.Referent},{RoleNames.CasaConvivenciaDirector},{RoleNames.CasaConvivenciaCoordinator}")]
    public async Task<IActionResult> GetMedicationHistoryPdf(
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
            MedicationHistoryError.PersonNotFound => NotFound(new { message = result.ErrorMessage }),
            not null => BadRequest(new { message = result.ErrorMessage }),
            _ => File(
                _medicationHistoryPdfGenerator.GeneratePersonHistoryPdf(new MedicationHistoryPdfDocument(
                    PersonName: result.PersonName ?? string.Empty,
                    PersonDni: result.PersonDni,
                    From: filter.From,
                    To: filter.To,
                    Entries: result.Entries ?? [],
                    GeneratedAt: DateTime.UtcNow)),
                "application/pdf",
                $"medication-history-{id}-{FormatPdfRange(filter.From, filter.To)}.pdf")
        };
    }

    private static string FormatPdfRange(DateOnly? from, DateOnly? to) =>
        (from, to) switch
        {
            (not null, not null) => $"{from.Value:yyyyMMdd}-{to.Value:yyyyMMdd}",
            (not null, null) => $"{from.Value:yyyyMMdd}-all",
            (null, not null) => $"all-{to.Value:yyyyMMdd}",
            _ => "all"
        };

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

    [HttpPost("{id}/attachments")]
    [HttpPost("/api/personas/{id}/adjuntos")]
    [Authorize(Roles = $"{RoleNames.Referent},{RoleNames.CasaConvivenciaDirector},{RoleNames.CasaConvivenciaCoordinator},{RoleNames.Listener}")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<IActionResult> UploadAttachments(Guid id, [FromForm] IFormFileCollection files, CancellationToken cancellationToken)
    {
        if (files is null || files.Count == 0)
        {
            return BadRequest(new { message = "Debe proporcionar al menos un archivo para adjuntar." });
        }

        var uploadItems = new List<UploadPersonAttachmentItem>();
        foreach (var file in files)
        {
            uploadItems.Add(new UploadPersonAttachmentItem(
                file.FileName,
                file.ContentType,
                file.Length,
                file.OpenReadStream()
            ));
        }

        var result = await _personAttachmentService.UploadAsync(id, uploadItems, ActorId, cancellationToken);

        return result.Error switch
        {
            null => StatusCode(StatusCodes.Status201Created, result.Attachments),
            UploadPersonAttachmentsError.PersonNotFound => NotFound(new { message = result.ErrorMessage }),
            UploadPersonAttachmentsError.NoFilesProvided => BadRequest(new { message = result.ErrorMessage }),
            UploadPersonAttachmentsError.InvalidFileType => BadRequest(new { message = result.ErrorMessage }),
            UploadPersonAttachmentsError.FileTooLarge => BadRequest(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }

    [HttpGet("{id}/attachments")]
    [HttpGet("/api/personas/{id}/adjuntos")]
    [Authorize(Roles = $"{RoleNames.Referent},{RoleNames.CasaConvivenciaDirector},{RoleNames.CasaConvivenciaCoordinator},{RoleNames.Listener}")]
    public async Task<IActionResult> GetAttachments(Guid id, CancellationToken cancellationToken)
    {
        var result = await _personAttachmentService.GetByPersonIdAsync(id, cancellationToken);

        return result.Error switch
        {
            null => Ok(result.Attachments),
            GetPersonAttachmentsError.PersonNotFound => NotFound(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }

    [HttpGet("{id}/attachments/{attachmentId:guid}")]
    [HttpGet("/api/personas/{id}/adjuntos/{attachmentId:guid}")]
    [Authorize(Roles = $"{RoleNames.Referent},{RoleNames.CasaConvivenciaDirector},{RoleNames.CasaConvivenciaCoordinator},{RoleNames.Listener}")]
    public async Task<IActionResult> GetAttachmentFile(Guid id, Guid attachmentId, [FromQuery] bool download = false, CancellationToken cancellationToken = default)
    {
        var result = await _personAttachmentService.GetFileAsync(id, attachmentId, cancellationToken);

        if (!result.Success || result.FileStream is null)
        {
            return result.Error switch
            {
                GetPersonAttachmentFileError.PersonNotFound => NotFound(new { message = result.ErrorMessage }),
                GetPersonAttachmentFileError.AttachmentNotFound => NotFound(new { message = result.ErrorMessage }),
                GetPersonAttachmentFileError.FileNotFoundOnDisk => NotFound(new { message = result.ErrorMessage }),
                _ => BadRequest(new { message = result.ErrorMessage })
            };
        }

        var fileDto = result.FileStream;
        if (download)
        {
            return File(fileDto.Stream, fileDto.ContentType, fileDto.FileName);
        }

        return File(fileDto.Stream, fileDto.ContentType);
    }

    [HttpDelete("{id}/attachments/{attachmentId:guid}")]
    [HttpDelete("/api/personas/{id}/adjuntos/{attachmentId:guid}")]
    [Authorize(Roles = RoleNames.Referent)]
    public async Task<IActionResult> DeleteAttachment(Guid id, Guid attachmentId, CancellationToken cancellationToken)
    {
        var result = await _personAttachmentService.DeleteAsync(id, attachmentId, ActorId, cancellationToken);

        return result.Error switch
        {
            null => NoContent(),
            DeletePersonAttachmentError.PersonNotFound => NotFound(new { message = result.ErrorMessage }),
            DeletePersonAttachmentError.AttachmentNotFound => NotFound(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }
    
    // campos del informe de Cáritas que faltan cargar en la ficha (SCRUM-273)
    [HttpGet("{id:guid}/report-fields/pending")]
    [HttpGet("/api/personas/{id:guid}/campos-informe-pendientes")]
    [DirectorResidentsOnly]
    [Authorize(Roles = $"{RoleNames.Referent},{RoleNames.CasaConvivenciaDirector}")]
    public async Task<IActionResult> GetPendingReportFields(Guid id, CancellationToken cancellationToken)
    {
        var pending = await _socialRecordService.GetPendingReportFieldsAsync(id, cancellationToken);
        return pending is null
            ? NotFound(new { message = "La ficha no existe." })
            : Ok(pending);
    }

    [HttpGet("{id}/psychiatric-evaluations/status")]
    [HttpGet("/api/personas/{id}/evaluacion-estado")]
    [DirectorResidentsOnly]
    [Authorize(Roles = $"{RoleNames.Referent},{RoleNames.CasaConvivenciaDirector},{RoleNames.CasaConvivenciaCoordinator},{RoleNames.Listener}")]
    public async Task<IActionResult> GetPsychiatricEvaluationStatus(Guid id, CancellationToken cancellationToken)
    {
        var result = await _psychiatricEvaluationService.GetStatusAsync(id, cancellationToken);

        return result.Error switch
        {
            null => Ok(result.Data),
            GetPsychiatricEvaluationStatusError.PersonNotFound => NotFound(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }

    [HttpGet("{id}/psychiatric-evaluations/history")]
    [HttpGet("/api/personas/{id}/evaluaciones-historial")]
    [DirectorResidentsOnly]
    [Authorize(Roles = $"{RoleNames.Referent},{RoleNames.CasaConvivenciaDirector},{RoleNames.CasaConvivenciaCoordinator},{RoleNames.Listener}")]
    public async Task<IActionResult> GetPsychiatricEvaluationHistory(Guid id, CancellationToken cancellationToken)
    {
        var result = await _psychiatricEvaluationService.GetHistoryAsync(id, cancellationToken);

        return result.Error switch
        {
            null => Ok(result.History),
            GetPsychiatricEvaluationHistoryError.PersonNotFound => NotFound(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }

    [HttpPatch("{id}/status")]
    [HttpPatch("/api/personas/{id}/estado")]
    [DirectorResidentsOnly]
    [Authorize(Roles = $"{RoleNames.Referent},{RoleNames.CasaConvivenciaDirector},{RoleNames.CasaConvivenciaCoordinator}")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdatePersonStatusDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await _updatePersonStatusValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return ValidationProblem(ModelState);
        }

        var result = await _socialRecordService.UpdateStatusAsync(id, dto, ActorId, cancellationToken);

        return result.Error switch
        {
            null => NoContent(),
            UpdatePersonStatusError.PersonNotFound => NotFound(new { message = result.ErrorMessage }),
            UpdatePersonStatusError.SocialRecordNotFound => NotFound(new { message = result.ErrorMessage }),
            UpdatePersonStatusError.SameStatus => Conflict(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }
}