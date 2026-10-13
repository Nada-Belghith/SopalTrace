using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.DTOs.Magasin;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Constants;

namespace SopalTrace.Api.Controllers;

/// <summary>
/// Magasin : préparation des OF et expédition des BL à la douchette. Chaque scan est un appel (enregistré aussitôt) ;
/// le contenu brut est lu et validé par le serveur. Le magasinier est celui du jeton.
/// </summary>
[Authorize(Roles = RolesApp.Admin + "," + RolesApp.Magasinier)]
[Route("api/magasinier")]
[ApiController]
public class MagasinierController : ControllerBase
{
    private readonly IMagasinService _service;

    public MagasinierController(IMagasinService service)
    {
        _service = service;
    }

    // ── Préparation des OF ──

    [HttpGet("of/{numeroOf}")]
    public async Task<IActionResult> GetOf(string numeroOf) => Ok(await _service.GetOfAsync(numeroOf));

    [HttpPost("preparations")]
    public async Task<IActionResult> OuvrirPreparation([FromBody] OuvrirPreparationRequest request) =>
        Ok(await _service.OuvrirPreparationAsync(request.NumeroOf));

    [HttpPost("preparations/{id:guid}/scans")]
    public async Task<IActionResult> ScannerComposant(Guid id, [FromBody] ScanRequest request) =>
        Ok(await _service.ScannerComposantAsync(id, request.Contenu));

    [HttpDelete("preparations/{id:guid}/scans/{lotId:guid}")]
    public async Task<IActionResult> RetirerLot(Guid id, Guid lotId) => Ok(await _service.RetirerLotAsync(id, lotId));

    [HttpPost("preparations/{id:guid}/terminer")]
    public async Task<IActionResult> TerminerPreparation(Guid id, [FromBody] TerminerPreparationRequest request) =>
        Ok(await _service.TerminerPreparationAsync(id, request.Motif));

    [HttpPost("preparations/{id:guid}/annuler")]
    public async Task<IActionResult> AnnulerPreparation(Guid id) => Ok(await _service.AnnulerPreparationAsync(id));

    // ── Expédition des BL ──

    [HttpGet("bl/{numeroBl}")]
    public async Task<IActionResult> GetBl(string numeroBl) => Ok(await _service.GetBlAsync(numeroBl));

    [HttpPost("expeditions")]
    public async Task<IActionResult> OuvrirExpedition([FromBody] OuvrirExpeditionRequest request) =>
        Ok(await _service.OuvrirExpeditionAsync(request.NumeroBl));

    [HttpPost("expeditions/{id:guid}/scans")]
    public async Task<IActionResult> ScannerOf(Guid id, [FromBody] ScanRequest request) =>
        Ok(await _service.ScannerOfAsync(id, request.Contenu));

    [HttpDelete("expeditions/{id:guid}/scans/{scanId:guid}")]
    public async Task<IActionResult> RetirerOf(Guid id, Guid scanId) => Ok(await _service.RetirerOfAsync(id, scanId));

    [HttpPost("expeditions/{id:guid}/terminer")]
    public async Task<IActionResult> TerminerExpedition(Guid id) => Ok(await _service.TerminerExpeditionAsync(id));
}
