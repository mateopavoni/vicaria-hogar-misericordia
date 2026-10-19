using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vicaria.Application.MedicationStocks;
using Vicaria.Domain.Entities;

namespace Vicaria.Api.Controllers;

[ApiController]
[Route("api/stock-medicamentos")]
[Authorize(Roles = $"{RoleNames.Referent},{RoleNames.CasaConvivenciaDirector}")]
public class MedicationStocksController : ControllerBase
{
    private readonly IMedicationStockService _stockService;
    private readonly IValidator<RegisterMedicationStockEntryDto> _entryValidator;
    private readonly IValidator<UpdateMedicationStockThresholdDto> _thresholdValidator;

    public MedicationStocksController(
        IMedicationStockService stockService,
        IValidator<RegisterMedicationStockEntryDto> entryValidator,
        IValidator<UpdateMedicationStockThresholdDto> thresholdValidator)
    {
        _stockService = stockService;
        _entryValidator = entryValidator;
        _thresholdValidator = thresholdValidator;
    }

    private Guid ActorId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<IActionResult> RegisterStockEntry(
        [FromBody] RegisterMedicationStockEntryDto dto,
        CancellationToken cancellationToken)
    {
        var validationResult = await _entryValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return ValidationProblem(ModelState);
        }

        var result = await _stockService.RegisterStockEntryAsync(dto, ActorId, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return CreatedAtAction(nameof(GetMovementsHistory), new { medicamentoId = result.MedicationId }, new { medicationId = result.MedicationId });
    }

    [HttpGet("{medicamentoId:guid}/movimientos")]
    public async Task<IActionResult> GetMovementsHistory([FromRoute] Guid medicamentoId, CancellationToken cancellationToken)
    {
        var result = await _stockService.GetMovementsByMedicationAsync(medicamentoId, cancellationToken);

        if (!result.Success)
        {
            return NotFound(new { message = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    [HttpGet("alertas")]
    public async Task<IActionResult> GetStockAlerts(CancellationToken cancellationToken)
    {
        var alerts = await _stockService.GetStockAlertsAsync(cancellationToken);
        return Ok(alerts);
    }

    [HttpPut("{medicamentoId:guid}/umbral")]
    public async Task<IActionResult> UpdateThreshold(
        [FromRoute] Guid medicamentoId,
        [FromBody] UpdateMedicationStockThresholdDto dto,
        CancellationToken cancellationToken)
    {
        var validationResult = await _thresholdValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return ValidationProblem(ModelState);
        }

        var result = await _stockService.UpdateThresholdAsync(medicamentoId, dto, ActorId, cancellationToken);

        if (!result.Success)
        {
            return NotFound(new { message = result.ErrorMessage });
        }

        return NoContent();
    }
}