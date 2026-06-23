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
public class ModeleFabricationController : ControllerBase
{
    private readonly IModeleFabricationService _modeleService;
    private readonly ILogger<ModeleFabricationController> _logger;

    public ModeleFabricationController(IModeleFabricationService modeleService, ILogger<ModeleFabricationController> logger)
    {
        _modeleService = modeleService;
        _logger = logger;
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
    public async Task<IActionResult> Create([FromBody] CreateDocumentRequestDto request)
    {
        try
        {
            var id = await _modeleService.CreerModeleAsync(request);
            return Ok(new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création du modèle.");
            return StatusCode(500, "Une erreur est survenue lors de la création.");
        }
    }

    [HttpPost("nouvelle-version")]
    public async Task<IActionResult> NouvelleVersion([FromBody] NouvelleVersionDocumentRequestDto request)
    {
        try
        {
            var id = await _modeleService.CreerNouvelleVersionModeleAsync(request);
            return Ok(new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création d'une nouvelle version.");
            return StatusCode(500, "Une erreur est survenue.");
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDocumentRequestDto request)
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
    public async Task<IActionResult> Restaurer([FromBody] RestaurerDocumentRequestDto request)
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
