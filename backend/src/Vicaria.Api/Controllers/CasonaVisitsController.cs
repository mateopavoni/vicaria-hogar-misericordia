using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vicaria.Application.CasonaVisits;
using Vicaria.Domain.Entities;

namespace Vicaria.Api.Controllers;

[ApiController]
[Route("api/casona-visits")]
[Authorize]
public class CasonaVisitsController : ControllerBase
{
    private readonly ICasonaVisitService _casonaVisitService;
    private readonly IValidator<CreateCasonaVisitDto> _createValidator;
    private readonly IValidator<UpdateCasonaVisitDto> _updateValidator;
    private readonly IValidator<ChangeCasonaVisitStatusDto> _statusValidator;

    public CasonaVisitsController(
        ICasonaVisitService casonaVisitService,
        IValidator<CreateCasonaVisitDto> createValidator,
        IValidator<UpdateCasonaVisitDto> updateValidator,
        IValidator<ChangeCasonaVisitStatusDto> statusValidator)
    {
        _casonaVisitService = casonaVisitService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _statusValidator = statusValidator;
    }

    private Guid ActorId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // escritura solo para Referente, Directora y Coordinador (SCRUM-210); Escucha consulta nomás
    [HttpPost]
    [Authorize(Roles = $"{RoleNames.Referent},{RoleNames.CasaConvivenciaDirector},{RoleNames.CasaConvivenciaCoordinator}")]
    public async Task<IActionResult> Create(
        [FromBody] CreateCasonaVisitDto dto,
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

        var result = await _casonaVisitService.CreateAsync(dto, ActorId, cancellationToken);
        return result.Error switch
        {
            null => StatusCode(StatusCodes.Status201Created, new { id = result.CasonaVisitId }),
            CreateCasonaVisitError.TimeOverlap => Conflict(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }

    // edición de una visita Pendiente; Realizada/Cancelada responden 409 (SCRUM-210)
    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{RoleNames.Referent},{RoleNames.CasaConvivenciaDirector},{RoleNames.CasaConvivenciaCoordinator}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateCasonaVisitDto dto,
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

        var result = await _casonaVisitService.UpdateAsync(id, dto, ActorId, cancellationToken);
        return result.Error switch
        {
            null => NoContent(),
            UpdateCasonaVisitError.NotFound => NotFound(new { message = result.ErrorMessage }),
            UpdateCasonaVisitError.InvalidState => Conflict(new { message = result.ErrorMessage }),
            UpdateCasonaVisitError.TimeOverlap => Conflict(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = $"{RoleNames.Referent},{RoleNames.CasaConvivenciaDirector},{RoleNames.CasaConvivenciaCoordinator}")]
    public async Task<IActionResult> ChangeStatus(
        Guid id,
        [FromBody] ChangeCasonaVisitStatusDto dto,
        CancellationToken cancellationToken)
    {
        var validationResult = await _statusValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return ValidationProblem(ModelState);
        }

        var result = await _casonaVisitService.ChangeStatusAsync(id, dto, ActorId, cancellationToken);
        return result.Error switch
        {
            null => NoContent(),
            ChangeCasonaVisitStatusError.NotFound => NotFound(new { message = result.ErrorMessage }),
            ChangeCasonaVisitStatusError.InvalidState => Conflict(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }

    // consulta por rango para el calendario semanal de visitas (SCRUM-210):
    // visible para los 4 roles, incluida Escucha (lectura)
    [HttpGet]
    [Authorize(Roles = $"{RoleNames.Referent},{RoleNames.CasaConvivenciaDirector},{RoleNames.Listener},{RoleNames.CasaConvivenciaCoordinator}")]
    public async Task<IActionResult> GetByRange(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var fromDate = from?.Date ?? DateTime.UtcNow.Date;
        var toDate = to?.Date ?? fromDate.AddDays(6);
        if (fromDate > toDate)
        {
            return BadRequest(new { message = "El parámetro 'from' no puede ser posterior a 'to'." });
        }

        var result = await _casonaVisitService.GetByRangeAsync(fromDate, toDate, page, cancellationToken);
        return Ok(result);
    }
}
