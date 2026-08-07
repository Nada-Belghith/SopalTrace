using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.DTOs.Execution.Operateur;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Interfaces.Execution;

namespace SopalTrace.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OperateurController : ControllerBase
{
    private readonly IExecutionOfService _executionOfService;
    private readonly IExecutionCatalogueService _executionCatalogueService;
    private readonly IAssemblageExecutionService _assemblageExecutionService;
    private readonly IVerifMachineExecutionService _verifMachineExecutionService;
    private readonly IOccurrenceService _occurrenceService;

    public OperateurController(
        IExecutionOfService executionOfService,
        IExecutionCatalogueService executionCatalogueService,
        IAssemblageExecutionService assemblageExecutionService,
        IVerifMachineExecutionService verifMachineExecutionService,
        IOccurrenceService occurrenceService)
    {
        _executionOfService = executionOfService;
        _executionCatalogueService = executionCatalogueService;
        _assemblageExecutionService = assemblageExecutionService;
        _verifMachineExecutionService = verifMachineExecutionService;
        _occurrenceService = occurrenceService;
    }

    [HttpGet("postes")]
    public async Task<IActionResult> GetPostesDisponibles()
    {
        var postes = await _executionCatalogueService.GetPostesDisponiblesAsync();
        return Ok(postes);
    }

    [HttpGet("poste/{posteCode}/machines")]
    public async Task<IActionResult> GetMachinesPourPoste(string posteCode)
    {
        var machines = await _executionCatalogueService.GetMachinesPourPosteAsync(posteCode);
        return Ok(machines);
    }

    [HttpGet("ofs/operations")]
    public async Task<IActionResult> GetAllOfOperations()
    {
        var ofs = await _executionCatalogueService.GetAllOfOperationsDisponiblesAsync();
        return Ok(ofs);
    }

    [HttpGet("plan/existe")]
    public async Task<IActionResult> VerifierPlanActif([FromQuery] string articleCode, [FromQuery] string? operationCode = null)
    {
        var result = await _executionOfService.VerifierPlanActifAsync(articleCode, operationCode);
        return Ok(result);
    }

    [HttpPut("plan/ligne/{ligneId:guid}")]
    public async Task<IActionResult> UpdatePlanLigne(Guid ligneId, [FromBody] UpdatePlanLigneDto dto)
    {
        var result = await _executionOfService.UpdatePlanLigneAsync(ligneId, dto);
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
        var result = await _executionOfService.IgnorerTrancheAsync(execControleOfId, trancheHoraire, request.MatriculeOperateur, request.Raison);
        if (!result) return NotFound(new { Message = "Tranche ou OF introuvable." });
        return Ok();
    }

    [HttpPost("of/{execControleOfId:guid}/tranche/{trancheHoraire}/reglage")]
    public async Task<IActionResult> DeclarerTrancheEnReglage(Guid execControleOfId, string trancheHoraire, [FromBody] IgnorerTrancheRequest request)
    {
        var result = await _executionOfService.DeclarerTrancheEnReglageAsync(execControleOfId, trancheHoraire, request.MatriculeOperateur);
        if (!result) return NotFound(new { Message = "Tranche ou OF introuvable." });
        return Ok();
    }

    [HttpPost("of/start")]
    public async Task<IActionResult> DemarrerOf([FromBody] DemarrerOfRequest request)
    {
        try
        {
            var result = await _executionOfService.DemarrerOfAsync(request);
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
        var result = await _executionOfService.MettreEnReglageAsync(execControleOfId);
        if (!result) return BadRequest(new { Message = "Impossible de mettre l'OF en réglage." });
        return Ok();
    }

    [HttpPost("of/{execControleOfId:guid}/pause")]
    public async Task<IActionResult> MettreEnPause(Guid execControleOfId, [FromBody] PauseOfRequest request)
    {
        var result = await _executionOfService.MettreEnPauseAsync(execControleOfId, request.Raison);
        if (!result) return BadRequest(new { Message = "Impossible de mettre l'OF en pause." });
        return Ok();
    }

    [HttpPost("of/{execControleOfId:guid}/reprendre")]
    public async Task<IActionResult> ReprendreDepuisPause(Guid execControleOfId)
    {
        var resultPause = await _executionOfService.ReprendreDepuisPauseAsync(execControleOfId);
        if (resultPause) return Ok();

        var resultReglage = await _executionOfService.ReprendreOfAsync(execControleOfId);
        if (resultReglage.Success) return Ok();

        return BadRequest(new { Message = resultReglage.Message ?? "Impossible de reprendre cet OF." });
    }

    [HttpPost("of/{execControleOfId:guid}/close")]
    public async Task<IActionResult> CloturerOf(Guid execControleOfId)
    {
        var result = await _executionOfService.CloturerOfAsync(execControleOfId);
        if (!result) return BadRequest(new { Message = "Impossible de clôturer cet OF." });
        return Ok();
    }

    [HttpGet("of/{execControleOfId:guid}/alertes-actives")]
    public async Task<IActionResult> GetAlertesActives(Guid execControleOfId, [FromQuery] string? posteCode = null)
    {
        var alertes = await _occurrenceService.GetAlertesActivesAsync(execControleOfId, posteCode);
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
            var result = await _assemblageExecutionService.DemarrerOfAssemblageAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPost("of/{execControleOfId:guid}/assemblage-documents/init/{typeDocument}")]
    public async Task<IActionResult> InitDocumentsAssemblage(Guid execControleOfId, string typeDocument, [FromQuery] string? posteCode, [FromQuery] string? machineCode, [FromQuery] string? equipe)
    {
        try
        {
            var matricule = User.FindFirst("matricule")?.Value;
            var result = await _assemblageExecutionService.InitDocumentsAsync(execControleOfId, typeDocument, posteCode, machineCode, equipe, matricule);
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
        var statuts = await _assemblageExecutionService.GetDocumentsAssemblageStatusAsync(execControleOfId);
        return Ok(statuts);
    }

    [HttpGet("postes/{posteCode}/machines")]
    public async Task<IActionResult> GetMachinesByPoste(string posteCode)
    {
        var machines = await _assemblageExecutionService.GetMachinesByPosteAsync(posteCode);
        return Ok(machines);
    }

    [HttpGet("machines")]
    public async Task<IActionResult> GetAllMachines()
    {
        var machines = await _executionCatalogueService.GetAllMachinesAsync();
        return Ok(machines);
    }

    [HttpPut("assemblage-documents/{statutId:guid}/terminer")]
    public async Task<IActionResult> MarquerDocumentTermine(Guid statutId, [FromQuery] string periodicite = "")
    {
        var matricule = User.FindFirst("matricule")?.Value;
        var result = await _assemblageExecutionService.MarquerDocumentTermineAsync(statutId, matricule, periodicite);
        if (!result) return NotFound(new { Message = "Statut document introuvable." });
        return Ok();
    }

    [HttpPut("assemblage-documents/{statutId:guid}/rouvrir")]
    public async Task<IActionResult> RouvrirDocument(Guid statutId, [FromQuery] string periodicite = "")
    {
        var result = await _assemblageExecutionService.RouvrirDocumentAsync(statutId, periodicite);
        if (!result) return NotFound(new { Message = "Statut document introuvable." });
        return Ok();
    }

    [HttpGet("ofs/assemblage-statut")]
    public async Task<IActionResult> GetOfsAssemblageStatut()
    {
        try
        {
            var result = await _assemblageExecutionService.GetOfsAssemblageStatutAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPost("of/{execControleOfId:guid}/ajouter-postes")]
    public async Task<IActionResult> AjouterPostes(Guid execControleOfId, [FromBody] List<string> posteCodes)
    {
        try
        {
            var result = await _assemblageExecutionService.AjouterPostesAsync(execControleOfId, posteCodes);
            if (!result) return NotFound(new { Message = "Exécution introuvable." });
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    // --- Endpoints pour Vérification Machine ---
    [HttpGet("verif-machine/periodicites")]
    public async Task<IActionResult> GetPeriodicitesMachine()
    {
        var periodes = await _verifMachineExecutionService.GetPeriodicitesMachineAsync();
        return Ok(periodes);
    }

    [HttpGet("verif-machine/statut/{statutId:guid}")]
    public async Task<IActionResult> GetAllExecVerifMachine(Guid statutId)
    {
        var exec = await _verifMachineExecutionService.GetExecVerifMachineAsync(statutId, null);
        return Ok(exec);
    }

    [HttpGet("verif-machine/statut/{statutId:guid}/periodicite/{periodiciteId:guid}")]
    public async Task<IActionResult> GetExecVerifMachine(Guid statutId, Guid periodiciteId)
    {
        var exec = await _verifMachineExecutionService.GetExecVerifMachineAsync(statutId, periodiciteId);
        return Ok(exec);
    }

    [HttpPost("verif-machine/save")]
    public async Task<IActionResult> SaveExecVerifMachine([FromBody] SopalTrace.Application.Dtos.VerifMachine.SaveExecVerifMachineRequest request)
    {
        try
        {
            var matricule = User.FindFirst("matricule")?.Value;
            if (string.IsNullOrEmpty(request.MatriculeOperateur) && !string.IsNullOrEmpty(matricule)) 
            {
                request.MatriculeOperateur = matricule;
            }

            var result = await _verifMachineExecutionService.SaveExecVerifMachineAsync(request);
            return Ok(new { success = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPut("of/{execControleOfId}/machine/{machineCode}/terminer-tout")]
    public async Task<IActionResult> TerminerToutDocumentsMachine(Guid execControleOfId, string machineCode)
    {
        try
        {
            var result = await _verifMachineExecutionService.TerminerToutDocumentsMachineAsync(execControleOfId, machineCode);
            return Ok(new { success = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPost("of/{execControleOfId}/verif-machine/cloturer-tous")]
    public async Task<IActionResult> CloturerTousVerifMachine(Guid execControleOfId, [FromQuery] string posteCode)
    {
        try
        {
            var result = await _verifMachineExecutionService.CloturerTousVerifMachineAsync(execControleOfId, posteCode);
            return Ok(new { success = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }
}
