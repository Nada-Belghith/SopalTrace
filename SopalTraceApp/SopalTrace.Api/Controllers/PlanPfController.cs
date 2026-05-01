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

    [HttpDelete("{id}")]
    public async Task<ActionResult> SupprimerBrouillon(Guid id)
    {
        try
        {
            // Appelle la logique de suppression du brouillon via le BasePlanLifecycleService
            await _planPfService.SupprimerBrouillonAsync(id);
            return Ok(new { message = "Brouillon supprimé." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
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


    [HttpPost("import-excel")]
    public async Task<ActionResult> ImportExcel([FromForm] Microsoft.AspNetCore.Http.IFormFile file, [FromServices] IExcelImportService excelService)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Aucun fichier sélectionné ou fichier vide." });

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Seuls les fichiers Excel (.xlsx) sont supportés." });

        try
        {
            using var stream = file.OpenReadStream();
            var parsedData = await excelService.ParsePlanExcelAsync(stream);
            return Ok(new { message = "Import réussi", data = parsedData });
        }
        catch (Exception ex)
        {
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            return StatusCode(500, new { message = "Erreur lors de l'import : " + innerMessage });
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
