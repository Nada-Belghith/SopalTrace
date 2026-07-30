using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SopalTrace.Application.DTOs.QualityPlans.Modeles;
using SopalTrace.Application.Interfaces;
using System;
using System.Threading.Tasks;

using SopalTrace.Domain.Constants;

namespace SopalTrace.Api.Controllers;

[Authorize(Roles = RolesApp.Admin + "," + RolesApp.ResponsableDI)]
[ApiController]
[Route("api/[controller]")]
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
            var modeles = await _modeleService.GetModelsByFiltersAsync(natureComposantCode, operationCode, familleProduitCode, statut);
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
            var modele = await _modeleService.GetModelByIdAsync(id);
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
            var id = await _modeleService.CreateModelAsync(request);
            return Ok(new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création du modèle.");
            return StatusCode(500, $"Une erreur est survenue lors de la création. Détails: {ex.Message} - Inner: {ex.InnerException?.Message}");
        }
    }

    [HttpPost("nouvelle-version")]
    public async Task<IActionResult> CreateNewVersion([FromBody] NouvelleVersionModeleRequestDto request)
    {
        try
        {
            var id = await _modeleService.CreateNewVersionAsync(request);
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
            var success = await _modeleService.UpdateModelAsync(id, request);
            if (!success) return NotFound();
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la mise à jour du modèle.");
            return StatusCode(500, "Une erreur est survenue.");
        }
    }

}
