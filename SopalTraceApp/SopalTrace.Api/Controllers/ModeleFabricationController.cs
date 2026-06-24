using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SopalTrace.Application.DTOs.QualityPlans.Modeles;
using SopalTrace.Application.Interfaces;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ModeleFabricationController : ControllerBase
{
    private readonly IModeleFabricationService _modeleService;
    private readonly ILogger<ModeleFabricationController> _logger;

    public ModeleFabricationController(IModeleFabricationService modeleService, ILogger<ModeleFabricationController> logger)
    {
        _modeleService = modeleService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetByFilters(
        [FromQuery] string? natureComposantCode = null,
        [FromQuery] string? operationCode = null,
        [FromQuery] string? familleProduitCode = null,
        [FromQuery] string? statut = null)
    {
        try
        {
            var modeles = await _modeleService.GetModelesByFiltersAsync(natureComposantCode, operationCode, familleProduitCode, statut);
            return Ok(modeles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des modèles avec filtres.");
            return StatusCode(500, "Une erreur est survenue.");
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var modele = await _modeleService.GetModeleByIdAsync(id);
            if (modele == null) return NotFound("Modèle non trouvé.");
            return Ok(modele);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération du modèle.");
            return StatusCode(500, "Une erreur est survenue.");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateModeleRequestDto request)
    {
        try
        {
            var id = await _modeleService.CreerModeleAsync(request);
            return Ok(new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création du modèle.");
            return StatusCode(500, $"Une erreur est survenue lors de la création. Détails: {ex.Message} - Inner: {ex.InnerException?.Message}");
        }
    }

    [HttpPost("nouvelle-version")]
    public async Task<IActionResult> NouvelleVersion([FromBody] NouvelleVersionModeleRequestDto request)
    {
        try
        {
            var id = await _modeleService.CreerNouvelleVersionModeleAsync(request);
            return Ok(new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création d'une nouvelle version.");
            return StatusCode(500, new { message = ex.Message, details = ex.ToString() });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateModeleRequestDto request)
    {
        try
        {
            var success = await _modeleService.MettreAJourModeleAsync(id, request);
            if (!success) return NotFound();
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la mise à jour du modèle.");
            return StatusCode(500, "Une erreur est survenue.");
        }
    }

    [HttpPost("restaurer")]
    public async Task<IActionResult> Restaurer([FromBody] RestaurerModeleRequestDto request)
    {
        try
        {
            var id = await _modeleService.RestaurerModeleArchiveAsync(request);
            return Ok(new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la restauration du modèle.");
            return StatusCode(500, "Une erreur est survenue.");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var success = await _modeleService.SupprimerModeleAsync(id);
            if (!success) return NotFound();
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la suppression du modèle.");
            return StatusCode(500, "Une erreur est survenue.");
        }
    }
}
