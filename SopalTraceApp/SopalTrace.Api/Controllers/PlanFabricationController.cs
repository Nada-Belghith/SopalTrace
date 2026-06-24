using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SopalTrace.Application.DTOs.QualityPlans.Documents;
using SopalTrace.Application.Interfaces;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PlanFabricationController : ControllerBase
{
    private readonly IPlanFabricationService _planService;
    private readonly ILogger<PlanFabricationController> _logger;

    public PlanFabricationController(IPlanFabricationService planService, ILogger<PlanFabricationController> logger)
    {
        _planService = planService;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var plan = await _planService.GetPlanByIdAsync(id);
            if (plan == null) return NotFound("Plan non trouvé.");
            return Ok(plan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération du plan.");
            return StatusCode(500, "Une erreur est survenue.");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetByFilters(
        [FromQuery] string? natureComposantCode = null, 
        [FromQuery] string? operationCode = null, 
        [FromQuery] string? familleProduitCode = null,
        [FromQuery] string? statut = null,
        [FromQuery] string? codeArticleSageVersionne = null)
    {
        try
        {
            var plans = await _planService.GetPlansByFiltersAsync(natureComposantCode, operationCode, familleProduitCode, statut, codeArticleSageVersionne);
            return Ok(plans);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des plans.");
            return StatusCode(500, "Une erreur est survenue.");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDocumentRequestDto request)
    {
        try
        {
            var id = await _planService.CreerPlanAsync(request);
            return Ok(new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création du plan.");
            return StatusCode(500, "Une erreur est survenue lors de la création.");
        }
    }

    [HttpPost("nouvelle-version")]
    public async Task<IActionResult> NouvelleVersion([FromBody] NouvelleVersionDocumentRequestDto request)
    {
        try
        {
            var id = await _planService.CreerNouvelleVersionPlanAsync(request);
            return Ok(new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création d'une nouvelle version.");
            return StatusCode(500, new { message = ex.Message, details = ex.ToString() });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDocumentRequestDto request)
    {
        try
        {
            var success = await _planService.MettreAJourPlanAsync(id, request);
            if (!success) return NotFound();
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la mise à jour du plan.");
            return StatusCode(500, "Une erreur est survenue.");
        }
    }

    [HttpPost("restaurer")]
    public async Task<IActionResult> Restaurer([FromBody] RestaurerDocumentRequestDto request)
    {
        try
        {
            var id = await _planService.RestaurerPlanArchiveAsync(request);
            return Ok(new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la restauration du plan.");
            return StatusCode(500, "Une erreur est survenue.");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var success = await _planService.SupprimerPlanAsync(id);
            if (!success) return NotFound();
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la suppression du plan.");
            return StatusCode(500, "Une erreur est survenue.");
        }
    }
}
