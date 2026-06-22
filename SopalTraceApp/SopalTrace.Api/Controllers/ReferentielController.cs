using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.DTOs.QualityPlans.Referentiels;
using SopalTrace.Application.Interfaces;
using System.Threading.Tasks;

namespace SopalTrace.Api.Controllers;

[Route("api/referentiels")]
[ApiController]
public class ReferentielController : ControllerBase
{
    private readonly ICatalogueReferentielService _referentielService;
    private readonly IFormulaireStructureService _formulaireService;

    public ReferentielController(ICatalogueReferentielService referentielService, IFormulaireStructureService formulaireService)
    {
        _referentielService = referentielService;
        _formulaireService = formulaireService;
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
    public async Task<IActionResult> GetDictionnairesControlePoste()
    {
        var data = await _referentielService.GetControlePosteReferentielsAsync();
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

    [HttpGet("articles-sf/search")]
    public async Task<IActionResult> SearchArticlesSf([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest("La requête de recherche est requise.");

        var articles = await _referentielService.SearchArticlesSfAsync(q);
        return Ok(articles);
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

    [HttpGet("formulaires/role/{role}")]
    public async Task<IActionResult> GetFormulaireByRole(string role)
    {
        var result = await _formulaireService.GetFormulaireByRoleAsync(role);
        if (result == null) return NotFound(new { success = false, message = $"Formulaire avec le role {role} introuvable ou inactif." });
        return Ok(new { success = true, data = result });
    }

    [HttpGet("formulaires/{id}")]
    public async Task<IActionResult> GetFormulaireById(Guid id)
    {
        var result = await _formulaireService.GetFormulaireByIdAsync(id);
        if (result == null) return NotFound(new { success = false, message = $"Formulaire {id} introuvable." });
        return Ok(new { success = true, data = result });
    }

    [HttpGet("formulaires/liste/{role}")]
    public async Task<IActionResult> GetFormulairesListByRole(string role)
    {
        var result = await _formulaireService.GetFormulairesListByRoleAsync(role);
        return Ok(new { success = true, data = result });
    }

    [HttpPut("formulaires/role/{role}")]
    public async Task<IActionResult> UpdateFormulaireStructure(string role, [FromBody] UpdateFormulaireStructureDto request)
    {
        var newId = await _formulaireService.UpdateFormulaireStructureAsync(role, request.ConfigurationStructureJson, null, request.VersionInitiale);
        if (newId == null) return NotFound(new { success = false, message = $"Formulaire avec le role {role} introuvable ou inactif." });
        return Ok(new { success = true, message = "Structure du formulaire mise a jour avec succes.", newId = newId });
    }

    [HttpPost("formulaires/{id}/activer")]
    public async Task<IActionResult> ActiverFormulaire(Guid id)
    {
        var result = await _formulaireService.ActiverFormulaireAsync(id);
        if (!result) return NotFound(new { success = false, message = "Formulaire introuvable ou n'est pas en statut BROUILLON." });
        return Ok(new { success = true, message = "Formulaire activé avec succès." });
    }

    [HttpPost("periodicites")]
    public async Task<IActionResult> CreatePeriodicite([FromBody] CreatePeriodiciteDto request)
    {
        try
        {
            var result = await _referentielService.CreatePeriodiciteAsync(request);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
