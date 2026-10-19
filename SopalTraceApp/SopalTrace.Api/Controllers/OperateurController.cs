using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SopalTrace.Api.Extensions;
using SopalTrace.Application.Dtos.VerifMachine;
using SopalTrace.Application.DTOs.Execution.Operateur;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Domain.Constants;

namespace SopalTrace.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = RolesApp.Admin + "," + RolesApp.Operateur)]
public class OperateurController : ControllerBase
{
    private readonly IExecutionOfService _executionOfService;
    private readonly IExecutionCatalogueService _executionCatalogueService;
    private readonly IAssemblageExecutionService _assemblageExecutionService;
    private readonly IVerifMachineExecutionService _verifMachineExecutionService;
    private readonly IOccurrenceService _occurrenceService;
    private readonly IAvancementAssemblageService _avancementAssemblage;
    private readonly IPlanificationProductionService _planification;

    public OperateurController(
        IExecutionOfService executionOfService,
        IExecutionCatalogueService executionCatalogueService,
        IAssemblageExecutionService assemblageExecutionService,
        IVerifMachineExecutionService verifMachineExecutionService,
        IOccurrenceService occurrenceService,
        IAvancementAssemblageService avancementAssemblage,
        IPlanificationProductionService planification)
    {
        _avancementAssemblage = avancementAssemblage;
        _planification = planification;
        _executionOfService = executionOfService;
        _executionCatalogueService = executionCatalogueService;
        _assemblageExecutionService = assemblageExecutionService;
        _verifMachineExecutionService = verifMachineExecutionService;
        _occurrenceService = occurrenceService;
    }

    // ───────────────────────────── Catalogue ─────────────────────────────

    [HttpGet("postes")]
    public async Task<IActionResult> GetPostesDisponibles()
    {
        return Ok(await _executionCatalogueService.GetPostesDisponiblesAsync());
    }

    [HttpGet("poste/{posteCode}/machines")]
    public async Task<IActionResult> GetMachinesPourPoste(
        [FromRoute, RegularExpression(FormatsEntree.Code, ErrorMessage = FormatsEntree.MessageCodeInvalide)] string posteCode)
    {
        return Ok(await _executionCatalogueService.GetMachinesPourPosteAsync(posteCode));
    }

    [HttpGet("machines")]
    public async Task<IActionResult> GetAllMachines()
    {
        return Ok(await _executionCatalogueService.GetAllMachinesAsync());
    }

    // ───────────────────────────── OF semi-finis (fabrication) ─────────────────────────────

    [HttpGet("ofs/operations")]
    public async Task<IActionResult> GetAllOfOperations()
    {
        return Ok(await _executionCatalogueService.GetAllOfOperationsDisponiblesAsync());
    }

    [HttpGet("plan/existe")]
    public async Task<IActionResult> VerifierPlanActif(
        [FromQuery, Required, StringLength(50)] string articleCode,
        [FromQuery, StringLength(20)] string? operationCode = null)
    {
        return Ok(await _executionOfService.VerifierPlanActifAsync(articleCode, operationCode));
    }

    [HttpPost("of/start")]
    public async Task<IActionResult> DemarrerOf([FromBody] DemarrerOfRequest request)
    {
        request.MatriculeOperateur = User.GetMatricule();
        return Ok(await _executionOfService.DemarrerOfAsync(request));
    }

    [HttpPost("of/{execControleOfId:guid}/mettre-en-reglage")]
    public async Task<IActionResult> MettreEnReglage(Guid execControleOfId)
    {
        if (!await _executionOfService.MettreEnReglageAsync(execControleOfId))
            return BadRequest(new { message = "Impossible de mettre l'OF en réglage." });
        return NoContent();
    }

    // ───────────────────────────── Cycle de vie commun ─────────────────────────────

    [HttpPost("of/{execControleOfId:guid}/pause")]
    public async Task<IActionResult> MettreEnPause(Guid execControleOfId, [FromBody] PauseOfRequest request)
    {
        if (!await _executionOfService.MettreEnPauseAsync(execControleOfId, request.Raison, request.TypePause, request.Equipe))
            return BadRequest(new { message = "Impossible de mettre l'OF en pause." });
        return NoContent();
    }

    [HttpPost("of/{execControleOfId:guid}/reprendre")]
    public async Task<IActionResult> Reprendre(Guid execControleOfId, [FromQuery, StringLength(10)] string? equipe = null)
    {
        if (await _executionOfService.ReprendreDepuisPauseAsync(execControleOfId, equipe)) return NoContent();

        var resultReglage = await _executionOfService.ReprendreOfAsync(execControleOfId);
        if (resultReglage.Success) return NoContent();

        return BadRequest(new { message = resultReglage.Message });
    }

    /// <summary>
    /// Fin de poste directe d'une équipe : le poste donné, sinon tous les postes qu'elle tient (contrôles de fin
    /// vérifiés, sauf pour un administrateur qui force la fin).
    /// </summary>
    [HttpPost("of/{execControleOfId:guid}/fin-poste")]
    public async Task<IActionResult> FinPoste(
        Guid execControleOfId,
        [FromQuery, StringLength(10)] string? equipe = null,
        [FromQuery, RegularExpression(FormatsEntree.Code, ErrorMessage = FormatsEntree.MessageCodeInvalide)] string? posteCode = null)
    {
        await _avancementAssemblage.TerminerPostesDirectementAsync(execControleOfId, equipe, posteCode, User.GetMatricule(),
            verifier: !User.IsInRole(RolesApp.Admin));
        return NoContent();
    }

    [HttpPost("of/{execControleOfId:guid}/close")]
    public async Task<IActionResult> CloturerOf(Guid execControleOfId)
    {
        await VerifierFinAssemblageAsync(execControleOfId, DemandesCloture.FinOf);
        if (!await _executionOfService.CloturerOfAsync(execControleOfId))
            return BadRequest(new { message = "Impossible de clôturer cet OF." });
        return NoContent();
    }

    // ───────────────────────────── Occurrences de contrôle ─────────────────────────────

    [HttpGet("of/{execControleOfId:guid}/alertes-actives")]
    public async Task<IActionResult> GetAlertesActives(
        Guid execControleOfId,
        [FromQuery, RegularExpression(FormatsEntree.Code, ErrorMessage = FormatsEntree.MessageCodeInvalide)] string? posteCode = null,
        [FromQuery, StringLength(10)] string? equipe = null)
    {
        // Assemblage : contrôles dus créés avant lecture (la tâche de fond les planifie aussi chaque minute).
        await _planification.SynchroniserAsync(execControleOfId);
        return Ok(await _occurrenceService.GetAlertesActivesAsync(execControleOfId, posteCode, equipe));
    }

    [HttpPost("occurrence/{occurrenceId:guid}/repondre")]
    public async Task<IActionResult> RepondreOccurrence(Guid occurrenceId, [FromBody] RepondreOccurrenceRequest request)
    {
        request.MatriculeOperateur = User.GetMatricule();
        if (!await _occurrenceService.RepondreOccurrenceAsync(occurrenceId, request))
            return BadRequest(new { message = "Impossible de traiter cette occurrence." });
        return NoContent();
    }

    [HttpPost("of/{execControleOfId:guid}/tranche/{trancheHoraire}/ignorer")]
    public async Task<IActionResult> IgnorerTranche(
        Guid execControleOfId,
        [FromRoute, StringLength(20)] string trancheHoraire,
        [FromBody] IgnorerTrancheRequest request)
    {
        if (!await _executionOfService.IgnorerTrancheAsync(execControleOfId, trancheHoraire, User.GetMatricule(), request.Raison))
            return NotFound(new { message = "Tranche ou OF introuvable." });
        return NoContent();
    }

    [HttpPost("of/{execControleOfId:guid}/tranche/{trancheHoraire}/reglage")]
    public async Task<IActionResult> DeclarerTrancheEnReglage(Guid execControleOfId, [FromRoute, StringLength(20)] string trancheHoraire)
    {
        if (!await _executionOfService.DeclarerTrancheEnReglageAsync(execControleOfId, trancheHoraire, User.GetMatricule()))
            return NotFound(new { message = "Tranche ou OF introuvable." });
        return NoContent();
    }

    // ───────────────────────────── OF produits finis (assemblage) ─────────────────────────────

    [HttpGet("ofs/assemblage-statut")]
    public async Task<IActionResult> GetOfsAssemblageStatut()
    {
        return Ok(await _assemblageExecutionService.GetOfsAssemblageStatutAsync());
    }

    [HttpPost("of/start-assemblage")]
    public async Task<IActionResult> DemarrerOfAssemblage([FromBody] DemarrerOfAssemblageRequest request)
    {
        return Ok(await _assemblageExecutionService.DemarrerOfAssemblageAsync(request, User.GetMatricule()));
    }

    [HttpPost("of/{execControleOfId:guid}/ajouter-postes")]
    public async Task<IActionResult> AjouterPostes(Guid execControleOfId, [FromBody] AjouterPostesRequest request)
    {
        await _assemblageExecutionService.AjouterPostesAsync(execControleOfId, request, User.GetMatricule());
        return NoContent();
    }

    [HttpGet("of/{execControleOfId:guid}/assemblage-documents")]
    public async Task<IActionResult> GetDocumentsAssemblageStatus(Guid execControleOfId, [FromQuery, StringLength(10)] string? equipe = null)
    {
        return Ok(await _assemblageExecutionService.GetDocumentsAssemblageStatusAsync(execControleOfId, equipe));
    }

    [HttpPut("assemblage-documents/{statutId:guid}/terminer")]
    public async Task<IActionResult> MarquerDocumentTermine(Guid statutId, [FromQuery, StringLength(20)] string? periodicite = null)
    {
        await _assemblageExecutionService.MarquerDocumentTermineAsync(statutId, User.GetMatricule(), periodicite);
        return NoContent();
    }

    [HttpPut("assemblage-documents/{statutId:guid}/rouvrir")]
    public async Task<IActionResult> RouvrirDocument(Guid statutId, [FromQuery, StringLength(20)] string? periodicite = null)
    {
        await _assemblageExecutionService.RouvrirDocumentAsync(statutId, User.GetMatricule(), periodicite);
        return NoContent();
    }

    // ───────────────────────────── Vérification machine ─────────────────────────────

    [HttpPost("of/{execControleOfId:guid}/verif-machine/init")]
    public async Task<IActionResult> InitVerifMachine(Guid execControleOfId, [FromBody] InitVerifMachineRequest request)
    {
        var statutId = await _assemblageExecutionService.InitVerifMachineAsync(execControleOfId, request, User.GetMatricule());
        return Ok(new { id = statutId });
    }

    [HttpGet("verif-machine/statut/{statutId:guid}")]
    public async Task<IActionResult> GetExecVerifMachine(Guid statutId)
    {
        return Ok(await _verifMachineExecutionService.GetExecVerifMachineAsync(statutId));
    }

    [HttpPost("verif-machine/save")]
    public async Task<IActionResult> SaveExecVerifMachine([FromBody] SaveExecVerifMachineRequest request)
    {
        request.MatriculeOperateur = User.GetMatricule();
        await _verifMachineExecutionService.SaveExecVerifMachineAsync(request);
        return NoContent();
    }

    [HttpPut("of/{execControleOfId:guid}/machine/{machineCode}/terminer-tout")]
    public async Task<IActionResult> TerminerToutDocumentsMachine(
        Guid execControleOfId,
        [FromRoute, RegularExpression(FormatsEntree.Code, ErrorMessage = FormatsEntree.MessageCodeInvalide)] string machineCode)
    {
        await _verifMachineExecutionService.TerminerToutDocumentsMachineAsync(execControleOfId, machineCode);
        return NoContent();
    }

    [HttpPost("of/{execControleOfId:guid}/verif-machine/cloturer-tous")]
    public async Task<IActionResult> CloturerTousVerifMachine(
        Guid execControleOfId,
        [FromQuery, Required, RegularExpression(FormatsEntree.Code, ErrorMessage = FormatsEntree.MessageCodeInvalide)] string posteCode)
    {
        await _verifMachineExecutionService.CloturerTousVerifMachineAsync(execControleOfId, posteCode);
        return NoContent();
    }

    /// <summary>
    /// OF d'assemblage : l'opérateur ne termine son poste (ou l'OF) que si les contrôles de fin sont faits sur
    /// tous les postes ; un administrateur peut forcer la fin (supervision).
    /// </summary>
    private async Task VerifierFinAssemblageAsync(Guid execControleOfId, string mode)
    {
        if (!User.IsInRole(RolesApp.Admin))
            await _avancementAssemblage.VerifierFinPossibleAsync(execControleOfId, mode);
    }
}
