using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.Interfaces;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Api.Controllers;

[Route("api/hub")]
[ApiController]
public class HubController : ControllerBase
{
    private readonly IHubService _service;

    public HubController(IHubService service)
    {
        _service = service;
    }

    [HttpGet("modeles")]
    public async Task<IActionResult> GetTousLesModeles()
    {
        var data = await _service.GetTousLesModelesAsync();
        return Ok(new { success = true, data });
    }

    [HttpGet("plans")]
    public async Task<IActionResult> GetTousLesPlans()
    {
        var data = await _service.GetTousLesPlansAsync();
        return Ok(new { success = true, data });
    }

    [HttpPut("modeles/{category}/{id}/statut")]
    public async Task<IActionResult> ChangerStatutModele(string category, Guid id, [FromQuery] string statut)
    {
        if (statut != "ACTIF" && statut != "ARCHIVE")
        {
            return BadRequest("Le statut doit être ACTIF ou ARCHIVE.");
        }

        var updated = await _service.ChangerStatutModeleAsync(category, id, statut);
        if (!updated)
        {
            return BadRequest("Catégorie inconnue ou modèle introuvable.");
        }

        return Ok(new { success = true, message = $"Le modèle a été passé en {statut}." });
    }

    [HttpPut("plans/{category}/{id}/statut")]
    public async Task<IActionResult> ChangerStatutPlan(string category, Guid id, [FromQuery] string statut)
    {
        if (statut != "ACTIF" && statut != "ARCHIVE")
        {
            return BadRequest("Le statut doit être ACTIF ou ARCHIVE.");
        }

        var updated = await _service.ChangerStatutPlanAsync(category, id, statut);
        if (!updated)
        {
            return BadRequest("Catégorie inconnue, plan introuvable ou action non autorisée (ex: archivage d'un brouillon). ");
        }

        return Ok(new { success = true, message = $"Le plan a été passé en {statut}." });
    }

    [HttpDelete("plans/{category}/{id}")]
    public async Task<IActionResult> SupprimerBrouillonPlan(string category, Guid id)
    {
        var deleted = await _service.SupprimerBrouillonPlanAsync(category, id);
        if (!deleted)
        {
            return BadRequest("Catégorie inconnue, plan introuvable ou plan non brouillon.");
        }

        return Ok(new { success = true, message = "Le brouillon a été retiré de la liste." });
    }
}