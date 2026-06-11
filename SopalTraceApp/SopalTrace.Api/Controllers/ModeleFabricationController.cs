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
        [FromQuery] string? operation,
        [FromQuery] string? poste,
        [FromQuery] string? familleProduit)
    {
        var data = await _modeleService.GetModelesByFiltersAsync(typeRobinet, natureComposant, operation, poste, familleProduit);
        return Ok(new { success = true, data });
    }

    [HttpPut("{id}/valeurs")]
    public async Task<IActionResult> UpdateModele(Guid id, [FromBody] CreateModeleRequestDto request)
    {
        await _modeleService.UpdateModeleBrouillonAsync(id, request);
        return Ok(new { success = true, message = "Modèle mis à jour." });
    }

    [HttpPost("{id}/activer")]
    public async Task<IActionResult> ActiverModele(Guid id)
    {
        await _modeleService.ActiverModeleAsync(id, "SYSTEM");
        return Ok(new { success = true, message = "Modèle activé." });
    }

    [HttpPost]
    public async Task<IActionResult> CreerModele([FromBody] CreateModeleRequestDto request)
    {
        var id = await _modeleService.CreerModeleAsync(request);
        var data = await _modeleService.GetModeleByIdAsync(id);

        return Ok(new { success = true, modeleId = id, version = data?.Version ?? 0, message = "Modèle créé et activé avec succès." });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> SupprimerBrouillon(Guid id)
    {
        var resultFab = await _modeleService.SupprimerBrouillonAsync(id);
        if (resultFab) return Ok(new { success = true, message = "Brouillon supprimé." });

        return NotFound(new { success = false, message = "Modèle introuvable." });
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
        var data = await _modeleService.GetModeleByIdAsync(id);

        return Ok(new { success = true, modeleId = id, version = data?.Version ?? 0, message = $"V{data?.Version ?? 0} du modèle générée avec succès." });
    }

    [HttpPost("restaurer")]
    public async Task<IActionResult> RestaurerModele([FromBody] RestaurerModeleRequestDto request)
    {
        try
        {
            var id = await _modeleService.RestaurerModeleArchiveAsync(request);
            var data = await _modeleService.GetModeleByIdAsync(id);

            return Ok(new { success = true, modeleId = id, version = data?.Version ?? 0, message = "Modèle restauré et activé avec succès." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("{id}/upgrade")]
    public async Task<IActionResult> UpgradeModele(Guid id)
    {
        try
        {
            var newId = await _modeleService.MettreANiveauModeleArchiveAsync(id);
            return Ok(new { success = true, modeleId = newId, message = "Modèle mis à niveau avec succès." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
