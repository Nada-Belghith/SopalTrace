using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.DTOs.QualityPlans.Modeles;
using SopalTrace.Application.DTOs.QualityPlans.Plans;
using SopalTrace.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Api.Controllers;

[Route("api/plans-fabrication")]
[ApiController]
public class PlanFabricationController : ControllerBase
{
    private readonly IPlanFabricationService _planService;

    public PlanFabricationController(IPlanFabricationService planService)
    {
        _planService = planService;
    }

    // ==========================================
    // MODÈLES (GABARITS)
    // ==========================================

    [HttpPost("modeles")]
    public async Task<IActionResult> CreerModele([FromBody] CreateModeleRequestDto request)
    {
        var id = await _planService.CreerModeleAsync(request);
        return Ok(new { success = true, modeleId = id });
    }

    [HttpGet("modeles/{id}")]
    public async Task<IActionResult> GetModele(Guid id)
    {
        var data = await _planService.GetModeleByIdAsync(id);
        return Ok(new { success = true, data });
    }

    // ==========================================
    // PLANS DE FABRICATION
    // ==========================================

    [HttpPost("plans/instancier")]
    public async Task<IActionResult> InstancierPlan([FromBody] CreatePlanRequestDto request)
    {
        var id = await _planService.InstancierPlanDepuisModeleAsync(request);
        return Ok(new { success = true, planId = id, message = "Plan instancié en BROUILLON." });
    }

    [HttpGet("plans/{id}")]
    public async Task<IActionResult> GetPlan(Guid id)
    {
        var data = await _planService.GetPlanByIdAsync(id);
        return Ok(new { success = true, data });
    }

    [HttpPut("plans/{id}/valeurs")]
    public async Task<IActionResult> MettreAJourValeurs(Guid id, [FromBody] List<SectionEditDto> request)
    {
        var success = await _planService.MettreAJourValeursPlanAsync(id, request);

        if (!success) return NotFound(new { success = false, message = "Plan introuvable." });

        return Ok(new { success = true, message = "Plan mis à jour avec liberté totale et rendu ACTIF." });
    }

    [HttpPost("plans/clone")]
    public async Task<IActionResult> ClonerPlan([FromBody] ClonePlanRequestDto request)
    {
        var id = await _planService.ClonerPlanPourNouvelArticleAsync(request);
        return Ok(new { success = true, planId = id, message = "Plan cloné avec succès." });
    }

    [HttpPost("plans/nouvelle-version")]
    public async Task<IActionResult> CreerVersion([FromBody] NouvelleVersionRequestDto request)
    {
        var id = await _planService.CreerNouvelleVersionPlanAsync(request);
        return Ok(new { success = true, planId = id, message = "V2 générée avec succès." });
    }
}