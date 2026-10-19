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

/// <summary>
/// Assistant de l'opérateur d'assemblage, poste par poste : étape courante, tâches, fin de poste contrôlée et
/// clôture de l'OF. L'équipe de la tablette est passée en paramètre (par défaut celle qui tient le poste).
/// </summary>
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

    /// <summary>L'équipe prend un poste libre de l'OF : ses contrôles de démarrage puis de fin de poste deviennent dus.</summary>
    [HttpPost("postes/{posteCode}/demarrer")]
    public async Task<IActionResult> DemarrerPoste(
        Guid execControleOfId,
        [FromRoute, RegularExpression(FormatsEntree.Code, ErrorMessage = FormatsEntree.MessageCodeInvalide)] string posteCode,
        [FromQuery, StringLength(10)] string? equipe = null)
    {
        await _service.DemarrerPosteAsync(execControleOfId, posteCode, equipe, User.GetMatricule());
        return NoContent();
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

    /// <summary>Fin de poste (ce poste seulement) ou clôture de l'OF (tous les postes, équipe seule sur l'OF).</summary>
    [HttpPost("demande-cloture")]
    public async Task<IActionResult> DemanderCloture(Guid execControleOfId, [FromBody] DemandeClotureRequest request)
    {
        await _service.DemanderClotureAsync(execControleOfId, request.Mode, request.PosteCode, request.Equipe);
        return NoContent();
    }

    [HttpDelete("demande-cloture")]
    public async Task<IActionResult> AnnulerDemandeCloture(
        Guid execControleOfId,
        [FromQuery, Required, RegularExpression(FormatsEntree.Code, ErrorMessage = FormatsEntree.MessageCodeInvalide)] string posteCode,
        [FromQuery, StringLength(10)] string? equipe = null)
    {
        await _service.AnnulerDemandeClotureAsync(execControleOfId, posteCode, equipe);
        return NoContent();
    }

    /// <summary>Termine le poste (ou clôture l'OF si elle est demandée) une fois les tâches de fin faites.</summary>
    [HttpPost("terminer")]
    public async Task<IActionResult> Terminer(
        Guid execControleOfId,
        [FromQuery, Required, RegularExpression(FormatsEntree.Code, ErrorMessage = FormatsEntree.MessageCodeInvalide)] string posteCode,
        [FromQuery, StringLength(10)] string? equipe = null)
    {
        await _service.TerminerAsync(execControleOfId, posteCode, equipe, User.GetMatricule());
        return NoContent();
    }
}
