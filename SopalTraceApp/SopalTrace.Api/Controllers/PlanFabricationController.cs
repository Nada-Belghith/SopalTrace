using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.DTOs.QualityPlans.Modeles;
using SopalTrace.Application.DTOs.QualityPlans.PlanFabrication;
using SopalTrace.Application.DTOs.QualityPlans.Referentiels;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Api.Controllers;

[Route("api/plans-fabrication")]
[ApiController]
public class PlanFabricationController : ControllerBase
{
    private readonly IPlanFabricationService _planService;
    private readonly SopalTraceDbContext _context;

    public PlanFabricationController(IPlanFabricationService planService, SopalTraceDbContext context)
    {
        _planService = planService;
        _context = context;
    }

    // ==========================================
    // MODÈLES
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

    [HttpPost("periodicites")]
    public async Task<IActionResult> CreerNouvellePeriodicite([FromBody] CreatePeriodiciteDto request)
    {
        var existeDeja = await _context.Periodicites.AnyAsync(x => x.Code == request.Code);

        if (existeDeja)
        {
            return BadRequest(new { success = false, message = "Une périodicité avec ce code existe déjà." });
        }

        var periodicite = new Periodicite
        {
            Id = Guid.NewGuid(),
            Code = request.Code,
            Libelle = request.Libelle,
            FrequenceNum = request.FrequenceNum,
            FrequenceUnite = request.FrequenceUnite,
            OrdreAffichage = request.OrdreAffichage,
            Actif = request.Actif
        };

        _context.Periodicites.Add(periodicite);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, periodiciteId = periodicite.Id });
    }
}