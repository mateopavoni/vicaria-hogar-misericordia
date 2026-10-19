using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vicaria.Application.Persons;
using Vicaria.Application.Attendances;
using Vicaria.Domain.Entities;

namespace Vicaria.Api.Controllers;

[ApiController]
[Route("api/attendances")]
[Authorize]
public class AttendancesController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly IAttendanceStatisticsService _statisticsService;
    private readonly IAttendanceSummaryPdfGenerator _pdfGenerator;
    private readonly IPersonAccessService _personAccessService;
    private readonly IValidator<CreateAttendanceDto> _validator;
    private readonly IValidator<GetInactivePeopleQueryDto> _inactiveValidator;
    private readonly IValidator<GetMonthlySummaryQueryDto> _monthlySummaryValidator;
    private readonly IValidator<GetStatisticsQueryDto> _statisticsValidator;

    public AttendancesController(
        IAttendanceService attendanceService,
        IAttendanceStatisticsService statisticsService,
        IAttendanceSummaryPdfGenerator pdfGenerator,
        IPersonAccessService personAccessService,
        IValidator<CreateAttendanceDto> validator,
        IValidator<GetInactivePeopleQueryDto> inactiveValidator,
        IValidator<GetMonthlySummaryQueryDto> monthlySummaryValidator,
        IValidator<GetStatisticsQueryDto> statisticsValidator)
    {
        _attendanceService = attendanceService;
        _statisticsService = statisticsService;
        _pdfGenerator = pdfGenerator;
        _personAccessService = personAccessService;
        _validator = validator;
        _inactiveValidator = inactiveValidator;
        _monthlySummaryValidator = monthlySummaryValidator;
        _statisticsValidator = statisticsValidator;
    }

    private Guid ActorId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // registra la asistencia diaria de una persona (SCRUM-216) y reactiva su ficha si
    // estaba Inactiva; Escucha también puede cargarla en el día a día del Centro Barrial
    [HttpPost]
    [Authorize(Roles = $"{RoleNames.Referent},{RoleNames.CasaConvivenciaDirector},{RoleNames.CasaConvivenciaCoordinator},{RoleNames.Listener}")]
    public async Task<IActionResult> Register([FromBody] CreateAttendanceDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return ValidationProblem(ModelState);
        }

        if (!await _personAccessService.CanAccessPersonAsync(dto.PersonId, User.FindFirstValue(ClaimTypes.Role), cancellationToken))
        {
            return NotFound();
        }

        var result = await _attendanceService.RegisterAsync(dto, ActorId, cancellationToken);

        return result.Error switch
        {
            null => NoContent(),
            RegisterAttendanceError.PersonNotFound => NotFound(new { message = result.ErrorMessage }),
            RegisterAttendanceError.SocialRecordNotFound => NotFound(new { message = result.ErrorMessage }),
            RegisterAttendanceError.Duplicate => Conflict(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }

    // lista las personas sin asistir en los últimos días (SCRUM-225);
    // criterio puro por asistencia, por defecto más de 14 días
    [HttpGet("inactive")]
    [Authorize(Roles = RoleNames.Referent)]
    public async Task<IActionResult> GetInactive([FromQuery] int days = 14, CancellationToken cancellationToken = default)
    {
        var query = new GetInactivePeopleQueryDto(days);
        var validationResult = await _inactiveValidator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return ValidationProblem(ModelState);
        }

        var result = await _statisticsService.GetInactivePeopleAsync(days, null, cancellationToken);
        return Ok(result.Data);
    }

    // estadísticas de asistencia por rango de fechas y actividad (SCRUM-224);
    // promedios por día + desglose semanal/mensual, totales por actividad y
    // comparativa contra el período anterior equivalente, solo Referente
    [HttpGet("statistics")]
    [Authorize(Roles = RoleNames.Referent)]
    public async Task<IActionResult> GetStatistics(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? activity,
        CancellationToken cancellationToken = default)
    {
        var toDate = (to ?? DateTime.UtcNow).Date;
        var fromDate = (from ?? toDate.AddDays(-29)).Date;
        var query = new GetStatisticsQueryDto(fromDate, toDate, activity);
        var validationResult = await _statisticsValidator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return ValidationProblem(ModelState);
        }

        var result = await _statisticsService.GetStatisticsAsync(query.From, query.To, query.Activity, cancellationToken);
        return Ok(result.Data);
    }

    // resumen mensual de asistencia exportable a PDF (SCRUM-225);
    // usa el mismo cálculo compartido que SCRUM-224 reutiliza
    [HttpGet("monthly-summary/pdf")]
    [Authorize(Roles = RoleNames.Referent)]
    public async Task<IActionResult> GetMonthlySummaryPdf([FromQuery] int? year, [FromQuery] int? month, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var query = new GetMonthlySummaryQueryDto(year ?? now.Year, month ?? now.Month);
        var validationResult = await _monthlySummaryValidator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return ValidationProblem(ModelState);
        }

        var result = await _statisticsService.GetMonthlySummaryAsync(query.Year, query.Month, cancellationToken);
        var pdfBytes = _pdfGenerator.GenerateMonthlySummaryPdf(result.Data!);
        return File(pdfBytes, "application/pdf", $"resumen-mensual-{query.Year}-{query.Month:00}.pdf");
    }
}