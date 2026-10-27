using System.Globalization;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vicaria.Application.CaritasReports;
using Vicaria.Domain.Entities;

namespace Vicaria.Api.Controllers;

[ApiController]
[Route("api/caritas-reports")]
[Authorize]
public class CaritasReportsController : ControllerBase
{
    private readonly ICaritasReportService _reportService;
    private readonly ICaritasReportPdfGenerator _pdfGenerator;
    private readonly IValidator<GetCaritasReportQueryDto> _queryValidator;

    public CaritasReportsController(
        ICaritasReportService reportService,
        ICaritasReportPdfGenerator pdfGenerator,
        IValidator<GetCaritasReportQueryDto> queryValidator)
    {
        _reportService = reportService;
        _pdfGenerator = pdfGenerator;
        _queryValidator = queryValidator;
    }

    // informe estadístico para Caritas (SCRUM-270): desglose por motivo de
    // asistencia, totales y resto de categorías; solo Referente
    [HttpGet]
    [Authorize(Roles = RoleNames.Referent)]
    public async Task<IActionResult> GetReport(
        [FromQuery] string type,
        [FromQuery] string to,
        CancellationToken cancellationToken)
    {
        var query = BuildQuery(type, to);
        if (query is null) return BadRequest(new { message = "La fecha de corte no es válida." });

        var validationResult = await _queryValidator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors) ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            return ValidationProblem(ModelState);
        }

        var result = await _reportService.GetReportAsync(query.Type, query.To, cancellationToken);
        return result.Error switch
        {
            null => Ok(result.Data),
            CaritasReportError.InvalidPeriod => BadRequest(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }

    // versión imprimible del informe en PDF (SCRUM-270); mismo contenido que el JSON
    [HttpGet("pdf")]
    [Authorize(Roles = RoleNames.Referent)]
    public async Task<IActionResult> GetReportPdf(
        [FromQuery] string type,
        [FromQuery] string to,
        CancellationToken cancellationToken)
    {
        var query = BuildQuery(type, to);
        if (query is null) return BadRequest(new { message = "La fecha de corte no es válida." });

        var validationResult = await _queryValidator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors) ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            return ValidationProblem(ModelState);
        }

        var result = await _reportService.GetReportAsync(query.Type, query.To, cancellationToken);
        if (result.Error is not null || result.Data is null)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        var pdfBytes = _pdfGenerator.GenerateReportPdf(result.Data);
        return File(pdfBytes, "application/pdf", $"informe-caritas-{result.Data.PeriodType}-{result.Data.To}.pdf");
    }

    private static GetCaritasReportQueryDto? BuildQuery(string type, string to)
    {
        if (!DateTime.TryParseExact(to, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var toDate))
        {
            return null;
        }

        return new GetCaritasReportQueryDto(type, toDate);
    }
}
