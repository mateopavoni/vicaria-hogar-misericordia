using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vicaria.Application.MedicationSchemes;
using Vicaria.Domain.Entities;

namespace Vicaria.Api.Controllers;

// consultas de esquemas de medicación que no cuelgan de una persona (SCRUM-249)
[ApiController]
[Route("api/medication-schemes")]
[Authorize]
public class MedicationSchemesController : ControllerBase
{
    private readonly IMedicationSchemeService _medicationSchemeService;

    public MedicationSchemesController(IMedicationSchemeService medicationSchemeService)
    {
        _medicationSchemeService = medicationSchemeService;
    }

    // catálogo de medicamentos con stock de farmacia para cargar esquemas
    [HttpGet("catalog")]
    [Authorize(Roles = $"{RoleNames.Referent},{RoleNames.CasaConvivenciaDirector},{RoleNames.CasaConvivenciaCoordinator}")]
    public async Task<IActionResult> GetCatalog(CancellationToken cancellationToken)
    {
        var catalog = await _medicationSchemeService.GetCatalogAsync(cancellationToken);
        return Ok(catalog);
    }

    // esquemas vigentes de todas las personas con más de 30 días sin revisión (SCRUM-249)
    [HttpGet("needs-review")]
    [Authorize(Roles = $"{RoleNames.Referent},{RoleNames.CasaConvivenciaDirector},{RoleNames.CasaConvivenciaCoordinator}")]
    public async Task<IActionResult> GetNeedsReview(CancellationToken cancellationToken)
    {
        var alerts = await _medicationSchemeService.GetNeedsReviewAsync(cancellationToken);
        return Ok(alerts);
    }
}
