using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.DTOs.QualityPlans.PlansEchantillonnage;
using SopalTrace.Application.Interfaces;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Api.Controllers;

[Route("api/plans-echantillonnage")]
[ApiController]
public class PlanEchantillonnageController : ControllerBase
{
    private readonly IPlanEchanService _service;

    public PlanEchantillonnageController(IPlanEchanService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePlanEchanRequestDto request)
    {
        var id = await _service.CreerPlanAsync(request, "ADMIN");
        return Ok(new { success = true, planId = id, message = "Profil d'échantillonnage créé et ACTIF." });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var data = await _service.GetPlanByIdAsync(id);
        return Ok(new { success = true, data });
    }

    // NOUVEAU : Endpoint pour modifier la V2 et l'activer
    [HttpPut("{id}")]
    public async Task<IActionResult> MettreAJourPlan(Guid id, [FromBody] UpdatePlanEchanRequestDto request)
    {
        var success = await _service.MettreAJourPlanAsync(id, request);
        if (!success) return NotFound(new { success = false, message = "Plan introuvable." });

        return Ok(new { success = true, message = "Plan mis à jour et activé avec succès. L'ancienne version a été archivée." });
    }

    [HttpPost("nouvelle-version")]
    public async Task<IActionResult> CreerVersion([FromBody] NouvelleVersionEchanRequestDto request)
    {
        var id = await _service.CreerNouvelleVersionAsync(request);
        return Ok(new { success = true, planId = id, message = "Nouvelle version générée et activée avec succès." });
    }

    [HttpPost("restaurer")]
    public async Task<IActionResult> Restaurer([FromBody] RestaurerEchanRequestDto request)
    {
        var id = await _service.RestaurerPlanAsync(request);
        return Ok(new { success = true, planId = id, message = "Plan restauré et activé avec succès." });
    }
}