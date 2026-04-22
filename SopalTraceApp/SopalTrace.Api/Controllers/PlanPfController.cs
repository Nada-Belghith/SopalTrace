using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.DTOs.QualityPlans.PlanProduitFini;
using SopalTrace.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Api.Controllers;

[ApiController]
[Route("api/plans-pf")]
public class PlanPfController : ControllerBase
{
    private readonly IPlanPfService _planPfService;

    public PlanPfController(IPlanPfService planPfService)
    {
        _planPfService = planPfService;
    }

    [HttpGet]
    public async Task<ActionResult<List<PlanPfEnteteDto>>> GetAll()
    {
        var plans = await _planPfService.GetGenericPlansAsync();
        return Ok(new { data = plans });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PlanPfEnteteDto>> GetById(Guid id)
    {
        var plan = await _planPfService.GetPlanByIdAsync(id);
        if (plan == null) return NotFound(new { message = "Plan introuvable." });
        return Ok(new { data = plan });
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreatePlanPfRequestDto dto)
    {
        try
        {
            var planId = await _planPfService.CreateGenericPlanAsync(dto, "Admin"); // TODO: Use real user
            return Ok(new { message = "Plan créé avec succès.", planId });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            return StatusCode(500, new { message = "Erreur création : " + innerMessage });
        }
    }

    [HttpPut("{id}/valeurs")]
    public async Task<ActionResult> UpdateValeurs(Guid id, [FromBody] List<SectionPfEditDto> sectionsDto)
    {
        try
        {
            await _planPfService.UpdatePlanAsync(id, sectionsDto, "Admin");
            return Ok(new { message = "Plan mis à jour avec succès." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            return StatusCode(500, new { message = "Erreur mise à jour : " + innerMessage });
        }
    }

    [HttpPost("{id}/nouvelle-version")]
    public async Task<ActionResult> NouvelleVersion(Guid id, [FromBody] NouvelleVersionPfRequestDto request)
    {
        try
        {
            if (id != request.AncienId) return BadRequest("L'ID de l'URL ne correspond pas à l'ancien ID du plan.");

            var newPlanId = await _planPfService.CreerNouvelleVersionAsync(request);
            return Ok(new { message = "Nouvelle version créée.", planId = newPlanId });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            return StatusCode(500, new { message = "Erreur de base de données : " + innerMessage });
        }
    }

    [HttpPost("restaurer")]
    public async Task<ActionResult> RestaurerPlan([FromBody] RestaurerPfRequestDto request)
    {
        try
        {
            var planId = await _planPfService.RestaurerPlanArchiveAsync(request);
            return Ok(new { message = "Plan restauré et activé avec succès.", planId });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            return StatusCode(500, new { message = "Erreur restauration : " + innerMessage });
        }
    }

    [HttpPut("{id}/statut")]
    public async Task<ActionResult> ChangerStatut(Guid id, [FromQuery] string statut)
    {
        try
        {
            if (statut == SopalTrace.Domain.Constants.StatutsPlan.Archive)
            {
                await _planPfService.ArchiverPlanAsync(id, "Admin");
                return Ok(new { message = "Plan archivé." });
            }
            return BadRequest(new { message = "Action non supportée par cet endpoint." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            return StatusCode(500, new { message = "Erreur statut : " + innerMessage });
        }
    }
}
