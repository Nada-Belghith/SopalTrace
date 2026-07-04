using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.DTOs.Execution.Operateur;
using SopalTrace.Application.Interfaces;

namespace SopalTrace.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OperateurController : ControllerBase
{
    private readonly IOperateurService _operateurService;
    private readonly IOccurrenceService _occurrenceService;

    public OperateurController(IOperateurService operateurService, IOccurrenceService occurrenceService)
    {
        _operateurService = operateurService;
        _occurrenceService = occurrenceService;
    }

    [HttpGet("postes")]
    public async Task<IActionResult> GetPostesDisponibles()
    {
        var postes = await _operateurService.GetPostesDisponiblesAsync();
        return Ok(postes);
    }

    [HttpGet("poste/{posteCode}/machines")]
    public async Task<IActionResult> GetMachinesPourPoste(string posteCode)
    {
        var machines = await _operateurService.GetMachinesPourPosteAsync(posteCode);
        return Ok(machines);
    }

    [HttpGet("ofs/operations")]
    public async Task<IActionResult> GetAllOfOperations()
    {
        var ofs = await _operateurService.GetAllOfOperationsDisponiblesAsync();
        return Ok(ofs);
    }

    [HttpGet("plan/existe")]
    public async Task<IActionResult> VerifierPlanActif([FromQuery] string articleCode)
    {
        var result = await _operateurService.VerifierPlanActifAsync(articleCode);
        return Ok(result);
    }

    [HttpPut("plan/ligne/{ligneId:guid}")]
    public async Task<IActionResult> UpdatePlanLigne(Guid ligneId, [FromBody] UpdatePlanLigneDto dto)
    {
        var result = await _operateurService.UpdatePlanLigneAsync(ligneId, dto);
        if (!result) return BadRequest(new { Message = "Impossible de mettre à jour la ligne du plan." });
        return Ok();
    }

    [HttpPost("of/{execControleOfId:guid}/tranche/{trancheHoraire}/ignorer")]
    public async Task<IActionResult> IgnorerTranche(Guid execControleOfId, string trancheHoraire)
    {
        var result = await _operateurService.IgnorerTrancheAsync(execControleOfId, trancheHoraire);
        if (!result) return NotFound(new { Message = "Tranche ou OF introuvable." });
        return Ok();
    }

    [HttpPost("of/start")]
    public async Task<IActionResult> DemarrerOf([FromBody] DemarrerOfRequest request)
    {
        try
        {
            var result = await _operateurService.DemarrerOfAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPost("of/{execControleOfId:guid}/mettre-en-reglage")]
    public async Task<IActionResult> MettreEnReglage(Guid execControleOfId)
    {
        var result = await _operateurService.MettreEnReglageAsync(execControleOfId);
        if (!result) return BadRequest(new { Message = "Impossible de mettre l'OF en réglage." });
        return Ok();
    }

    [HttpPost("of/{execControleOfId:guid}/pause")]
    public async Task<IActionResult> MettreEnPause(Guid execControleOfId)
    {
        var result = await _operateurService.MettreEnPauseAsync(execControleOfId);
        if (!result) return BadRequest(new { Message = "Impossible de mettre l'OF en pause." });
        return Ok();
    }

    [HttpPost("of/{execControleOfId:guid}/reprendre")]
    public async Task<IActionResult> ReprendreDepuisPause(Guid execControleOfId)
    {
        var result = await _operateurService.ReprendreDepuisPauseAsync(execControleOfId);
        if (!result) return BadRequest(new { Message = "Impossible de reprendre cet OF." });
        return Ok();
    }

    [HttpPost("of/{execControleOfId:guid}/close")]
    public async Task<IActionResult> CloturerOf(Guid execControleOfId)
    {
        var result = await _operateurService.CloturerOfAsync(execControleOfId);
        if (!result) return BadRequest(new { Message = "Impossible de clôturer cet OF." });
        return Ok();
    }

    [HttpGet("of/{execControleOfId:guid}/alertes-actives")]
    public async Task<IActionResult> GetAlertesActives(Guid execControleOfId)
    {
        var alertes = await _occurrenceService.GetAlertesActivesAsync(execControleOfId);
        return Ok(alertes);
    }

    [HttpPost("occurrence/{occurrenceId:guid}/repondre")]
    public async Task<IActionResult> RepondreOccurrence(Guid occurrenceId, [FromBody] RepondreOccurrenceRequest request)
    {
        var result = await _occurrenceService.RepondreOccurrenceAsync(occurrenceId, request);
        if (!result) return BadRequest(new { Message = "Impossible de traiter cette occurrence." });
        return Ok();
    }

    [HttpPost("occurrences/ignorer")]
    public async Task<IActionResult> IgnorerOccurrences([FromBody] IgnorerOccurrencesRequest request)
    {
        var result = await _occurrenceService.IgnorerOccurrencesAsync(request.OccurrenceIds, request.MatriculeOperateur);
        if (!result) return BadRequest(new { Message = "Erreur lors de la mise à jour des occurrences." });
        return Ok();
    }
}
