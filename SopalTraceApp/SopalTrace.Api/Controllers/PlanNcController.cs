using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.DTOs.QualityPlans.PlansNC;
using SopalTrace.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Api.Controllers;

[ApiController]
[Route("api/plans-nc")]
public class PlanNcController : ControllerBase
{
    private readonly IPlanNcService _service;

    public PlanNcController(IPlanNcService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePlanNcRequestDto request)
    {
        // En production, "ADMIN" sera remplacé par le matricule extrait du Token JWT
        var id = await _service.CreerPlanAsync(request, "ADMIN");
        return Ok(new { success = true, planId = id, message = "Plan NC créé en BROUILLON." });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var data = await _service.GetPlanByIdAsync(id);
        return Ok(new { success = true, data });
    }

    [HttpPut("{id}/colonnes")]
    public async Task<IActionResult> UpdateColonnes(Guid id, [FromBody] List<ColonneNcEditDto> colonnes)
    {
        var ok = await _service.MettreAJourColonnesAsync(id, colonnes);
        if (!ok) return NotFound(new { success = false, message = "Plan introuvable." });
        return Ok(new { success = true, message = "Colonnes synchronisées. Le plan est ACTIF." });
    }

    [HttpPost("nouvelle-version")]
    public async Task<IActionResult> NouvelleVersion([FromBody] NouvelleVersionNcRequestDto request)
    {
        var id = await _service.CreerNouvelleVersionAsync(request);
        return Ok(new { success = true, planId = id, message = "Nouvelle version générée en BROUILLON." });
    }
}
