using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.DTOs.QualityPlans.Fabrication;
using SopalTrace.Application.DTOs.QualityPlans.Documents;
using SopalTrace.Application.Interfaces;
using System;
using System.Threading.Tasks;

using SopalTrace.Domain.Constants;

namespace SopalTrace.Api.Controllers;

/// <summary>Plans de fabrication par article. Les erreurs sont journalisées et traduites par ExceptionMiddleware.</summary>
[Authorize(Roles = RolesApp.Admin + "," + RolesApp.ResponsableDI)]
[ApiController]
[Route("api/[controller]")]
public class PlanFabricationController : ControllerBase
{
    private readonly IPlanFabricationService _planService;

    public PlanFabricationController(IPlanFabricationService planService)
    {
        _planService = planService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var plan = await _planService.GetPlanByIdAsync(id);
        if (plan == null) return NotFound("Plan non trouvé.");
        return Ok(plan);
    }

    [HttpGet]
    public async Task<IActionResult> GetByFilters(
        [FromQuery] string? natureComposantCode = null,
        [FromQuery] string? operationCode = null,
        [FromQuery] string? familleProduitCode = null,
        [FromQuery] string? statut = null,
        [FromQuery] string? codeArticleSageVersionne = null)
    {
        return Ok(await _planService.GetPlansByFiltersAsync(natureComposantCode, operationCode, familleProduitCode, statut, codeArticleSageVersionne));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePlanFabricationRequestDto request)
    {
        var id = await _planService.CreatePlanAsync(request);
        return Ok(new { id });
    }

    [HttpPost("nouvelle-version")]
    public async Task<IActionResult> NouvelleVersion([FromBody] NouvelleVersionPlanFabricationRequestDto request)
    {
        var id = await _planService.CreateNewVersionAsync(request);
        return Ok(new { id });
    }

    [HttpPost("{id}/upgrade")]
    public async Task<IActionResult> UpgradePlanArchive(Guid id)
    {
        var newId = await _planService.UpgradeArchivedPlanAsync(id);
        return Ok(new { id = newId });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePlanFabricationRequestDto request)
    {
        var rowVersion = await _planService.UpdatePlanAsync(id, request);
        if (rowVersion == null) return NotFound();
        return Ok(new { success = true, rowVersion });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _planService.DeletePlanAsync(id);
        if (!success) return NotFound();
        return Ok(new { success = true });
    }
}
