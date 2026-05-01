using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.DTOs.QualityPlans.Modeles;
using SopalTrace.Application.Interfaces;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Api.Controllers;

[Route("api/modeles-fabrication")]
[ApiController]
public class ModeleFabricationController : ControllerBase
{
    private readonly IModeleFabricationService _modeleService;

    public ModeleFabricationController(IModeleFabricationService modeleService)
    {
        _modeleService = modeleService;
    }

    [HttpGet("liste")]
    public async Task<IActionResult> GetModelesByFilters(
        [FromQuery] string? typeRobinet, 
        [FromQuery] string? natureComposant, 
        [FromQuery] string? operation)
    {
        var data = await _modeleService.GetModelesByFiltersAsync(typeRobinet, natureComposant, operation);
        return Ok(new { success = true, data });
    }

    [HttpPost]
    public async Task<IActionResult> CreerModele([FromBody] CreateModeleRequestDto request)
    {
        var id = await _modeleService.CreerModeleAsync(request);
        return Ok(new { success = true, modeleId = id, message = "Modèle créé et activé avec succès." });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> SupprimerBrouillon(Guid id)
    {
        var result = await _modeleService.SupprimerBrouillonAsync(id);
        if (!result) return NotFound(new { success = false, message = "Modèle introuvable." });
        return Ok(new { success = true, message = "Brouillon supprimé." });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetModele(Guid id)
    {
        var data = await _modeleService.GetModeleByIdAsync(id);
        return Ok(new { success = true, data });
    }

    [HttpPost("nouvelle-version")]
    public async Task<IActionResult> CreerVersionModele([FromBody] NouvelleVersionModeleRequestDto request)
    {
        var id = await _modeleService.CreerNouvelleVersionModeleAsync(request);
        return Ok(new { success = true, modeleId = id, message = "V2 du modèle générée avec succès." });
    }

    [HttpPost("restaurer")]
    public async Task<IActionResult> RestaurerModele([FromBody] RestaurerModeleRequestDto request)
    {
        try
        {
            var id = await _modeleService.RestaurerModeleArchiveAsync(request);
            return Ok(new { success = true, modeleId = id, message = "Modèle restauré et activé avec succès." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
