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
    public async Task<IActionResult> VerifierPlanActif([FromQuery] string articleCode, [FromQuery] string? operationCode = null)
    {
        var result = await _operateurService.VerifierPlanActifAsync(articleCode, operationCode);
        return Ok(result);
    }

    [HttpPut("plan/ligne/{ligneId:guid}")]
    public async Task<IActionResult> UpdatePlanLigne(Guid ligneId, [FromBody] UpdatePlanLigneDto dto)
    {
        var result = await _operateurService.UpdatePlanLigneAsync(ligneId, dto);
        if (!result) return BadRequest(new { Message = "Impossible de mettre à jour la ligne du plan." });
        return Ok();
    }


    public class IgnorerTrancheRequest
    {
        public string MatriculeOperateur { get; set; } = null!;
        public string? Raison { get; set; }
    }

    public class PauseOfRequest
    {
        public string Raison { get; set; } = null!;
    }

    [HttpPost("of/{execControleOfId:guid}/tranche/{trancheHoraire}/ignorer")]
    public async Task<IActionResult> IgnorerTranche(Guid execControleOfId, string trancheHoraire, [FromBody] IgnorerTrancheRequest request)
    {
        var result = await _operateurService.IgnorerTrancheAsync(execControleOfId, trancheHoraire, request.MatriculeOperateur, request.Raison);
        if (!result) return NotFound(new { Message = "Tranche ou OF introuvable." });
        return Ok();
    }

    [HttpPost("of/{execControleOfId:guid}/tranche/{trancheHoraire}/reglage")]
    public async Task<IActionResult> DeclarerTrancheEnReglage(Guid execControleOfId, string trancheHoraire, [FromBody] IgnorerTrancheRequest request)
    {
        var result = await _operateurService.DeclarerTrancheEnReglageAsync(execControleOfId, trancheHoraire, request.MatriculeOperateur);
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
    public async Task<IActionResult> MettreEnPause(Guid execControleOfId, [FromBody] PauseOfRequest request)
    {
        var result = await _operateurService.MettreEnPauseAsync(execControleOfId, request.Raison);
        if (!result) return BadRequest(new { Message = "Impossible de mettre l'OF en pause." });
        return Ok();
    }

    [HttpPost("of/{execControleOfId:guid}/reprendre")]
    public async Task<IActionResult> ReprendreDepuisPause(Guid execControleOfId)
    {
        var resultPause = await _operateurService.ReprendreDepuisPauseAsync(execControleOfId);
        if (resultPause) return Ok();

        var resultReglage = await _operateurService.ReprendreOfAsync(execControleOfId);
        if (resultReglage.Success) return Ok();

        // Si ce n'était ni une pause, ni un réglage réussi, ou si la validation bloque
        return BadRequest(new { Message = resultReglage.Message ?? "Impossible de reprendre cet OF." });
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

    // --- Endpoints pour OF d'Assemblage ---
    [HttpPost("of/start-assemblage")]
    public async Task<IActionResult> DemarrerOfAssemblage([FromBody] DemarrerOfAssemblageRequest request)
    {
        try
        {
            var result = await _operateurService.DemarrerOfAssemblageAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPost("of/{execControleOfId:guid}/assemblage-documents/init/{typeDocument}")]
    public async Task<IActionResult> InitDocumentsAssemblage(Guid execControleOfId, string typeDocument)
    {
        try
        {
            var result = await _operateurService.InitDocumentsAsync(execControleOfId, typeDocument);
            return Ok(new { initialized = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet("of/{execControleOfId:guid}/assemblage-documents")]
    public async Task<IActionResult> GetDocumentsAssemblageStatus(Guid execControleOfId)
    {
        var statuts = await _operateurService.GetDocumentsAssemblageStatusAsync(execControleOfId);
        return Ok(statuts);
    }

    [HttpPut("assemblage-documents/{statutId:guid}/terminer")]
    public async Task<IActionResult> MarquerDocumentTermine(Guid statutId)
    {
        var result = await _operateurService.MarquerDocumentTermineAsync(statutId);
        if (!result) return NotFound(new { Message = "Statut document introuvable." });
        return Ok();
    }
}
