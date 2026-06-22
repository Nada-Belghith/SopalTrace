using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.DTOs;
using SopalTrace.Application.DTOs.QualityPlans.PlansEchantillonnage;
using SopalTrace.Application.Interfaces;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Api.Controllers;

[ApiController]
[Route("api/plans-echantillonnage")]
public class PlanEchantillonnageController : ControllerBase
{
    private readonly IPlanEchantillonnageService _service;

    public PlanEchantillonnageController(IPlanEchantillonnageService service)
    {
        _service = service;
    }

    [HttpGet("actif")]
    public async Task<IActionResult> GetPlanActif()
    {
        var plan = await _service.GetPlanActifAsync();
        return Ok(new { success = true, data = plan, message = plan == null ? "Aucun plan actif trouvé." : "Plan actif récupéré avec succès." });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPlanById(Guid id)
    {
        var plan = await _service.GetPlanByIdAsync(id);
        if (plan == null) return NotFound(new { success = false, message = "Plan introuvable." });
        return Ok(new { success = true, data = plan, message = "Plan récupéré avec succès." });
    }

    [HttpPost]
    public async Task<IActionResult> CreatePlan([FromBody] CreatePlanEchanRequestDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var creePar = "Admin"; // TODO: Utiliser ICurrentUserService si disponible
        var newId = await _service.CreatePlanAsync(request, creePar);

        return Ok(new { success = true, data = newId, message = "Plan créé avec succès." });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] UpdatePlanEchanRequestDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        await _service.UpdatePlanAsync(id, request);
        return Ok(new { success = true, data = id, message = "Plan mis à jour avec succès." });
    }

    [HttpPut("{id}/activer")]
    public async Task<IActionResult> ActiverPlan(Guid id)
    {
        var modifiePar = "Admin"; // TODO: Utiliser ICurrentUserService si disponible
        await _service.ActiverPlanAsync(id, modifiePar);
        return Ok(new { success = true, data = id, message = "Plan activé avec succès." });
    }

    [HttpPost("nouvelle-version")]
    public async Task<IActionResult> CreerNouvelleVersion([FromBody] NouvelleVersionEchanRequestDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var newId = await _service.CreerNouvelleVersionAsync(request);
        return Ok(new { success = true, data = newId, message = "Nouvelle version créée avec succès." });
    }

    [HttpPost("restaurer")]
    public async Task<IActionResult> RestaurerPlan([FromBody] RestaurerEchanRequestDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var newId = await _service.RestaurerPlanAsync(request);
        return Ok(new { success = true, data = newId, message = "Plan restauré avec succès." });
    }
}
