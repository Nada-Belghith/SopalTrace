using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.DTOs.QualityPlans.VerifMachine;
using SopalTrace.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Api.Controllers;

/// <summary>
/// Controller pour les plans de vérification machine (BEE, MAS, SER...).
/// Route principale : /api/plans-verif-machine
/// </summary>
[ApiController]
[Route("api/plans-verif-machine")]
public class PlanVerifMachineController : ControllerBase
{
    private readonly IPlanVerifMachineService _service;
    private readonly IValidator<CreateVerifMachineModeleDto> _validator;

    public PlanVerifMachineController(IPlanVerifMachineService service, IValidator<CreateVerifMachineModeleDto> validator)
    {
        _service = service;
        _validator = validator;
    }

    // =========================================================================
    // POST /api/plans-verif-machine
    // Création unifiée : reçoit l'ensemble du payload (flags + familles + lignes)
    // =========================================================================
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVerifMachineModeleDto request)
    {
        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(new { success = false, message = "Données invalides.", details = errors });
        }

        try
        {
            var id = await _service.CreerPlanVerifAsync(request, "ADMIN");
            return Ok(new { success = true, planId = id, message = "Plan de vérification machine créé avec succès." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // =========================================================================
    // GET /api/plans-verif-machine/{id}
    // =========================================================================
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        try
        {
            var data = await _service.GetPlanVerifByIdAsync(id);
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
    }

    // =========================================================================
    // PUT /api/plans-verif-machine/{id}
    // Mise à jour complète (remplace tout l'arbre)
    // =========================================================================
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateVerifMachineModeleDto request)
    {
        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(new { success = false, message = "Données invalides.", details = errors });
        }

        try
        {
            var newId = await _service.MettreAJourPlanVerifAsync(id, request, "ADMIN");
            return Ok(new { success = true, planId = newId, message = "Nouvelle version du plan créée et l'ancienne a été archivée." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // =========================================================================
    // POST /api/plans-verif-machine/{id}/nouvelle-version
    // =========================================================================
    [HttpPost("{id:guid}/nouvelle-version")]
    public async Task<IActionResult> NouvelleVersion(Guid id, [FromBody] NouvelleVersionVerifMachineDto request)
    {
        try
        {
            var newId = await _service.CreerNouvelleVersionAsync(request with { AncienId = id });
            return Ok(new { success = true, planId = newId, message = "Nouvelle version créée." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // =========================================================================
    // PUT /api/plans-verif-machine/{id}/statut
    // =========================================================================
    [HttpPut("{id:guid}/statut")]
    public async Task<IActionResult> ChangerStatut(Guid id, [FromQuery] string statut)
    {
        try
        {
            if (statut == "ARCHIVE")
            {
                await _service.ArchiverPlanAsync(id, "ADMIN"); // TODO: récupérer depuis JWT
                return Ok(new { success = true, message = "Plan archivé." });
            }
            return BadRequest(new { success = false, message = "Action non supportée." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // =========================================================================
    // POST /api/plans-verif-machine/restaurer
    // =========================================================================
    [HttpPost("restaurer")]
    public async Task<IActionResult> Restaurer([FromBody] NouvelleVersionVerifMachineDto request)
    {
        try
        {
            var newId = await _service.RestaurerPlanAsync(request.AncienId, request.ModifiePar ?? "ADMIN", request.MotifModification);
            return Ok(new { success = true, planId = newId, message = "Plan restauré." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // =========================================================================
    // GET /api/plans-verif-machine
    // Récupère tous les plans
    // =========================================================================
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var data = await _service.GetTousLesPlansVerifAsync();
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // =========================================================================
    // ROUTES LEGACY
    // =========================================================================
    [HttpPut("{id:guid}/valeurs")]
    public async Task<IActionResult> UpdateFullTree(Guid id, [FromBody] List<VerifMachineLigneEditDto> lignes)
    {
        var ok = await _service.MettreAJourValeursPlanAsync(id, lignes);
        if (!ok) return NotFound(new { success = false, message = "Plan introuvable." });
        return Ok(new { success = true, message = "Arbre synchronisé." });
    }
}
