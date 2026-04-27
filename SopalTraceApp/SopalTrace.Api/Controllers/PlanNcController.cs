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

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var data = await _service.GetTousLesPlansAsync();
        return Ok(new { success = true, data });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePlanNcRequestDto request)
    {
        var id = await _service.CreerPlanAsync(request, "ADMIN");
        return Ok(new { success = true, planId = id, message = "Plan NC créé et activé." });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var data = await _service.GetPlanByIdAsync(id);
        return Ok(new { success = true, data });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SavePlanNcDto request)
    {
        var newId = await _service.MettreAJourPlanAsync(id, request, "ADMIN");
        return Ok(new { success = true, planId = newId, message = "Nouvelle version enregistrée et activée." });
    }

    [HttpPost("nouvelle-version")]
    public async Task<IActionResult> NouvelleVersion([FromBody] NouvelleVersionNcRequestDto request)
    {
        var id = await _service.CreerNouvelleVersionAsync(request);
        return Ok(new { success = true, planId = id, message = "Nouvelle version générée en BROUILLON." });
    }

    [HttpPost("restaurer")]
    public async Task<IActionResult> Restaurer([FromBody] NouvelleVersionNcRequestDto request)
    {
        var id = await _service.RestaurerPlanAsync(request.AncienId, request.ModifiePar ?? "ADMIN", request.MotifModification);
        return Ok(new { success = true, planId = id, message = "Plan restauré avec succès." });
    }
}
