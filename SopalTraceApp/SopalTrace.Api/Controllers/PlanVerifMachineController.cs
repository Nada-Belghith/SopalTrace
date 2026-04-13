using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.DTOs.QualityPlans.VerifMachine;
using SopalTrace.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Api.Controllers;

[ApiController]
[Route("api/plans-verifmachine")]
public class PlanVerifMachineController : ControllerBase
{
    private readonly IPlanVerifMachineService _service;

    public PlanVerifMachineController(IPlanVerifMachineService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePlanVerifMachineDto request)
    {
        var id = await _service.CreerPlanAsync(request, "ADMIN"); // En prod: User JWT
        return Ok(new { success = true, planId = id, message = "Plan de vérification créé en BROUILLON." });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var data = await _service.GetPlanByIdAsync(id);
        return Ok(new { success = true, data });
    }

    [HttpPut("{id}/valeurs")]
    public async Task<IActionResult> UpdateFullTree(Guid id, [FromBody] List<VerifMachineLigneEditDto> lignes)
    {
        var ok = await _service.MettreAJourValeursPlanAsync(id, lignes);
        if (!ok) return NotFound(new { success = false, message = "Plan introuvable." });

        return Ok(new { success = true, message = "Arbre de vérification synchronisé et plan ACTIF." });
    }

    [HttpPost("nouvelle-version")]
    public async Task<IActionResult> NouvelleVersion([FromBody] NouvelleVersionVerifMachineDto request)
    {
        var id = await _service.CreerNouvelleVersionAsync(request);
        return Ok(new { success = true, planId = id, message = "V2 générée avec succès." });
    }
}
