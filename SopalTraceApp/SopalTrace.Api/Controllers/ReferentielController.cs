using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.DTOs.QualityPlans.Referentiels;
using SopalTrace.Application.Interfaces;
using System.Threading.Tasks;

namespace SopalTrace.Api.Controllers;

[Route("api/referentiels")]
[ApiController]
public class ReferentielController : ControllerBase
{
    private readonly IReferentielService _referentielService;

    public ReferentielController(IReferentielService referentielService)
    {
        _referentielService = referentielService;
    }

    [HttpGet("fabrication")]
    public async Task<IActionResult> GetDictionnairesFabrication([FromQuery] string? natureComposantCode = null, [FromQuery] string? operationCode = null)
    {
        var data = await _referentielService.GetFabricationReferentielsAsync(natureComposantCode, operationCode);
        return Ok(new { success = true, data });
    }

    [HttpGet("verif-machine")]
    public async Task<IActionResult> GetDictionnairesVerifMachine()
    {
        var data = await _referentielService.GetVerifMachineReferentielsAsync();
        return Ok(new { success = true, data });
    }

    [HttpGet("plans-nc")]
    public async Task<IActionResult> GetDictionnairesPlanNc()
    {
        var data = await _referentielService.GetPlanNcReferentielsAsync();
        return Ok(new { success = true, data });
    }

    [HttpGet("article/{codeArticle}")]
    public async Task<IActionResult> GetArticle(string codeArticle)
    {
        if (string.IsNullOrWhiteSpace(codeArticle))
            return BadRequest("Le code article est requis.");

        var article = await _referentielService.GetArticleInfosAsync(codeArticle);

        if (article == null)
            return NotFound($"Aucun article trouve pour le code '{codeArticle}' dans l'ERP.");

        return Ok(article);
    }

    /// <summary>
    /// Cree une nouvelle piece de reference (PRC, PRNC) ou un etalon fuite (FEC, FENC)
    /// directement depuis le formulaire Verification Machine.
    /// </summary>
    [HttpPost("piece-reference")]
    public async Task<IActionResult> CreatePieceReference([FromBody] CreatePieceReferenceDto request)
    {
        try
        {
            var result = await _referentielService.CreatePieceReferenceAsync(request);
            return Ok(new { success = true, data = result });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { success = false, message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}