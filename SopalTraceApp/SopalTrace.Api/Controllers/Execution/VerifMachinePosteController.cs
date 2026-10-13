using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SopalTrace.Api.Extensions;
using SopalTrace.Application.DTOs.Execution.Operateur;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Domain.Constants;

namespace SopalTrace.Api.Controllers.Execution;

/// <summary>Vérifications machine d'un poste d'assemblage : machines à vérifier, validation par moment, historique.</summary>
[ApiController]
[Route("api/operateur")]
[Authorize(Roles = RolesApp.Admin + "," + RolesApp.Operateur)]
public class VerifMachinePosteController : ControllerBase
{
    private const string Periodicite = "^(DEMARRAGE|APRES_PAUSE|FIN_POSTE)$";
    private readonly IVerifMachinePosteService _service;

    public VerifMachinePosteController(IVerifMachinePosteService service)
    {
        _service = service;
    }

    [HttpGet("of/{execControleOfId:guid}/poste/{posteCode}/verif-machines")]
    public async Task<ActionResult<VerifMachinesPosteDto>> GetMachines(
        Guid execControleOfId,
        [FromRoute, RegularExpression(FormatsEntree.Code, ErrorMessage = FormatsEntree.MessageCodeInvalide)] string posteCode,
        [FromQuery, Required, RegularExpression(Periodicite, ErrorMessage = "Périodicité invalide.")] string periodicite)
    {
        return Ok(await _service.GetMachinesAsync(execControleOfId, posteCode, periodicite));
    }

    [HttpPost("of/{execControleOfId:guid}/poste/{posteCode}/verif-machines")]
    public async Task<IActionResult> AjouterMachine(
        Guid execControleOfId,
        [FromRoute, RegularExpression(FormatsEntree.Code, ErrorMessage = FormatsEntree.MessageCodeInvalide)] string posteCode,
        [FromBody] AjouterMachinePosteRequest request)
    {
        var statutId = await _service.AjouterMachineAsync(execControleOfId, posteCode, request.MachineCode, User.GetMatricule());
        return Ok(new { id = statutId });
    }

    [HttpGet("of/{execControleOfId:guid}/poste/{posteCode}/verif-machines/historique")]
    public async Task<ActionResult<List<VerifMachineHistoriqueDto>>> GetHistorique(
        Guid execControleOfId,
        [FromRoute, RegularExpression(FormatsEntree.Code, ErrorMessage = FormatsEntree.MessageCodeInvalide)] string posteCode,
        [FromQuery, RegularExpression(FormatsEntree.Code, ErrorMessage = FormatsEntree.MessageCodeInvalide)] string? machineCode = null,
        [FromQuery, StringLength(10)] string? equipe = null,
        [FromQuery] DateTime? jour = null)
    {
        return Ok(await _service.GetHistoriqueAsync(execControleOfId, posteCode, machineCode, equipe, jour));
    }

    [HttpPost("verif-machine/{statutId:guid}/valider")]
    public async Task<IActionResult> Valider(Guid statutId, [FromBody] ValiderVerifMachineRequest request)
    {
        await _service.ValiderAsync(statutId, request, User.GetMatricule());
        return NoContent();
    }

    [HttpPost("verif-machine/{statutId:guid}/corriger")]
    public async Task<IActionResult> Corriger(
        Guid statutId,
        [FromQuery, Required, RegularExpression(Periodicite, ErrorMessage = "Périodicité invalide.")] string periodicite)
    {
        await _service.CorrigerAsync(statutId, periodicite, User.GetMatricule());
        return NoContent();
    }
}
