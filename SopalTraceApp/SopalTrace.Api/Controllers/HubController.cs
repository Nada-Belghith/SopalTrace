using SopalTrace.Domain.Constants;
using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.Interfaces;
using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;

namespace SopalTrace.Api.Controllers;

[Authorize(Roles = RolesApp.Admin + "," + RolesApp.ResponsableDI + "," + RolesApp.ResponsableQualite + "," + RolesApp.SuperviseurQualite)]
[Route("api/hub")]
[ApiController]
public class HubController : ControllerBase
{
    private readonly IHubService _service;

    public HubController(IHubService service)
    {
        _service = service;
    }

    /// <summary>Compteurs du tableau de bord superviseur : documents génériques / spécifiques par statut et par type.</summary>
    [HttpGet("statistiques")]
    public async Task<IActionResult> GetStatistiques() => Ok(await _service.GetStatistiquesAsync());

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

    [HttpGet("structures")]
    public async Task<IActionResult> GetToutesLesStructures()
    {
        var data = await _service.GetToutesLesStructuresAsync();
        return Ok(new { success = true, data });
    }

    /// <summary>
    /// Archiver, réactiver ou supprimer un brouillon : le superviseur qualité (documents qualité), le responsable DI pour la
    /// fabrication seulement, l'administrateur. Le responsable qualité consulte sans modifier.
    /// </summary>
    private const string RolesModification = RolesApp.Admin + "," + RolesApp.SuperviseurQualite + "," + RolesApp.ResponsableDI;
    private const string CategorieFabrication = "FAB";

    private bool HorsDeSonPerimetre(string category) =>
        User.IsInRole(RolesApp.ResponsableDI) && !string.Equals(category, CategorieFabrication, StringComparison.OrdinalIgnoreCase);

    [Authorize(Roles = RolesModification)]
    [HttpPut("modeles/{category}/{id}/statut")]
    public async Task<IActionResult> ChangerStatutModele(string category, Guid id, [FromQuery] string statut)
    {
        if (HorsDeSonPerimetre(category)) return Forbid();
        if (statut != StatutsPlan.Actif && statut != StatutsPlan.Archive)
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

    [Authorize(Roles = RolesModification)]
    [HttpPut("plans/{category}/{id}/statut")]
    public async Task<IActionResult> ChangerStatutPlan(string category, Guid id, [FromQuery] string statut)
    {
        if (HorsDeSonPerimetre(category)) return Forbid();
        if (statut != StatutsPlan.Actif && statut != StatutsPlan.Archive)
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

    [Authorize(Roles = RolesModification)]
    [HttpDelete("plans/{category}/{id}")]
    public async Task<IActionResult> SupprimerBrouillonPlan(string category, Guid id)
    {
        if (HorsDeSonPerimetre(category)) return Forbid();
        var deleted = await _service.SupprimerBrouillonPlanAsync(category, id);
        if (!deleted)
        {
            return BadRequest("Catégorie inconnue, plan introuvable ou plan non brouillon.");
        }

        return Ok(new { success = true, message = "Le brouillon a été retiré de la liste." });
    }
}
