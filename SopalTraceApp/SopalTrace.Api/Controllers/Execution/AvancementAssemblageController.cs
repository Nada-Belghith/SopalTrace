using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.DTOs.Execution.Operateur;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Api.Extensions;
using SopalTrace.Domain.Constants;

namespace SopalTrace.Api.Controllers.Execution;

/// <summary>Assistant de l'opérateur d'assemblage : étape courante, tâches et fin contrôlée de la session.</summary>
[ApiController]
[Route("api/operateur/of/{execControleOfId:guid}/assemblage")]
[Authorize(Roles = RolesApp.Admin + "," + RolesApp.Operateur)]
public class AvancementAssemblageController : ControllerBase
{
    private readonly IAvancementAssemblageService _service;
    private readonly IPlanificationProductionService _planification;

    public AvancementAssemblageController(IAvancementAssemblageService service, IPlanificationProductionService planification)
    {
        _service = service;
        _planification = planification;
    }

    [HttpGet("avancement")]
    public async Task<ActionResult<AvancementAssemblageDto>> GetAvancement(
        Guid execControleOfId,
        [FromQuery, Required, RegularExpression(FormatsEntree.Code, ErrorMessage = FormatsEntree.MessageCodeInvalide)] string posteCode,
        [FromQuery, StringLength(10)] string? equipe = null)
    {
        // Contrôles de production à jour avant lecture (la tâche de fond les planifie aussi chaque minute).
        await _planification.SynchroniserAsync(execControleOfId);
        return Ok(await _service.GetAvancementAsync(execControleOfId, posteCode, equipe));
    }

    /// <summary>Réglage commencé sur le poste : les contrôles de production s'arrêtent jusqu'à un réglage conforme.</summary>
    [HttpPost("reglage")]
    public async Task<IActionResult> DemarrerReglage(
        Guid execControleOfId,
        [FromQuery, Required, RegularExpression(FormatsEntree.Code, ErrorMessage = FormatsEntree.MessageCodeInvalide)] string posteCode)
    {
        await _planification.DemarrerReglageAsync(execControleOfId, posteCode);
        return NoContent();
    }

    /// <summary>Réglage abandonné avant toute saisie : la production du poste reprend.</summary>
    [HttpDelete("reglage")]
    public async Task<IActionResult> AnnulerReglage(
        Guid execControleOfId,
        [FromQuery, Required, RegularExpression(FormatsEntree.Code, ErrorMessage = FormatsEntree.MessageCodeInvalide)] string posteCode)
    {
        await _planification.AnnulerReglageAsync(execControleOfId, posteCode);
        return NoContent();
    }

    [HttpPost("demande-cloture")]
    public async Task<IActionResult> DemanderCloture(Guid execControleOfId, [FromBody] DemandeClotureRequest request)
    {
        await _service.DemanderClotureAsync(execControleOfId, request.Mode);
        return NoContent();
    }

    [HttpDelete("demande-cloture")]
    public async Task<IActionResult> AnnulerDemandeCloture(Guid execControleOfId)
    {
        await _service.AnnulerDemandeClotureAsync(execControleOfId);
        return NoContent();
    }

    [HttpPost("terminer")]
    public async Task<IActionResult> Terminer(Guid execControleOfId)
    {
        await _service.TerminerAsync(execControleOfId);
        return NoContent();
    }
}
