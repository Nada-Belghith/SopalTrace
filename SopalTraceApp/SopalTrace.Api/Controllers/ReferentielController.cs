using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.DTOs.QualityPlans.Referentiels;
using SopalTrace.Application.Interfaces;
using System.Threading.Tasks;

using SopalTrace.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using SopalTrace.Api.Extensions;

namespace SopalTrace.Api.Controllers;

[Authorize(Roles = RolesApp.Admin + "," + RolesApp.ResponsableDI + "," + RolesApp.ResponsableQualite + "," + RolesApp.SuperviseurQualite + "," + RolesApp.Operateur)]
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

    [HttpGet("instruments")]
    public async Task<IActionResult> GetInstruments()
    {
        var data = await _referentielService.GetInstrumentsAsync();
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
    [Authorize(Roles = RolesApp.Admin + "," + RolesApp.ResponsableDI + "," + RolesApp.ResponsableQualite + "," + RolesApp.SuperviseurQualite)]
    public async Task<IActionResult> CreatePieceReference([FromBody] CreatePieceReferenceDto request)
    {
        var result = await _referentielService.CreatePieceReferenceAsync(request);
        return Ok(new { success = true, data = result });
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

    [HttpGet("equipes")]
    public async Task<IActionResult> GetEquipes()
    {
        return Ok(new { success = true, data = await _formulaireService.GetEquipesReferenceAsync() });
    }

    [HttpGet("formulaires/liste/{role}")]
    public async Task<IActionResult> GetFormulairesListByRole(string role)
    {
        var result = await _formulaireService.GetFormulairesListByRoleAsync(role);
        return Ok(new { success = true, data = result });
    }

    [HttpPut("formulaires/role/{role}")]
    [Authorize(Roles = RolesApp.Admin + "," + RolesApp.ResponsableDI + "," + RolesApp.ResponsableQualite + "," + RolesApp.SuperviseurQualite)]
    public async Task<IActionResult> UpdateFormulaireStructure(string role, [FromBody] UpdateFormulaireStructureDto request)
    {
        var newId = await _formulaireService.UpdateFormulaireStructureAsync(role, request.ConfigurationStructureJson);
        if (newId == null) return NotFound(new { success = false, message = $"Formulaire avec le role {role} introuvable ou inactif." });
        return Ok(new { success = true, message = "Structure du formulaire mise a jour avec succes.", newId = newId });
    }

    public sealed record CodeFormulaireRequest(string Code);
    public sealed record CreationFormulaireRequest(string Role, string Code, string? Designation, string? FamilleProduitFiniCode,
        string? PosteCode, string? MachineCode, string? NatureArticleCode, string? TypeRobinetCode = null, int? Version = null);

    /// <summary>Rôles des formulaires : libellé, ordre d'affichage et critères permis du champ d'application.</summary>
    [HttpGet("formulaires/roles")]
    [Authorize]
    public async Task<IActionResult> RolesDesFormulaires() =>
        Ok(new { success = true, data = await _formulaireService.GetRolesAsync() });

    /// <summary>Listes du champ d'application d'un nouveau formulaire : familles, postes, machines, natures.</summary>
    [HttpGet("formulaires/champs-application")]
    [Authorize(Roles = RolesApp.SuperviseurQualite)]
    public async Task<IActionResult> ListesChampApplication() =>
        Ok(new { success = true, data = await _formulaireService.GetListesChampApplicationAsync() });

    /// <summary>Documents génériques en cours d'un formulaire : ceux qu'un changement de code ou de logo recrée (liste à confirmer).</summary>
    [HttpGet("formulaires/{id:guid}/documents-actifs")]
    [Authorize(Roles = RolesApp.SuperviseurQualite + "," + RolesApp.Admin)]
    public async Task<IActionResult> DocumentsActifsFormulaire(Guid id) =>
        Ok(new { success = true, data = await _formulaireService.GetDocumentsActifsAsync(id) });

    /// <summary>
    /// Le superviseur qualité change le code d'un formulaire (même version) : l'ancien formulaire et ses documents sont
    /// archivés tels quels, le nouveau formulaire reçoit les mêmes documents recréés (traçabilité code + version).
    /// </summary>
    [HttpPut("formulaires/{id:guid}/code")]
    [Authorize(Roles = RolesApp.SuperviseurQualite)]
    public async Task<IActionResult> RenommerCodeFormulaire(Guid id, [FromBody] CodeFormulaireRequest request)
    {
        var changement = await _formulaireService.ChangerCodeAsync(id, request.Code, User.GetMatricule());
        return Ok(new { success = true, data = changement, message = changement.DocumentsRecrees > 0
            ? $"Formulaire {changement.Code} version {changement.Version} : {changement.DocumentsRecrees} document(s) recréé(s) ; l'ancien formulaire et ses documents sont archivés tels quels."
            : $"Formulaire {changement.Code} version {changement.Version}." });
    }

    /// <summary>Le superviseur qualité crée le formulaire d'un champ d'application (famille, poste, machine…).</summary>
    [HttpPost("formulaires")]
    [Authorize(Roles = RolesApp.SuperviseurQualite)]
    public async Task<IActionResult> CreerFormulaire([FromBody] CreationFormulaireRequest request)
    {
        static string? Valeur(string? texte) => string.IsNullOrWhiteSpace(texte) ? null : texte.Trim();
        var champ = new SopalTrace.Application.Helpers.ChampFormulaire(request.Role, Valeur(request.FamilleProduitFiniCode),
            Valeur(request.PosteCode), Valeur(request.MachineCode), Valeur(request.NatureArticleCode), Valeur(request.TypeRobinetCode));
        var id = await _formulaireService.CreerFormulaireAsync(champ, request.Code, request.Designation, request.Version ?? 0);
        return Ok(new { success = true, message = $"Formulaire {request.Code.Trim().ToUpperInvariant()} créé.", data = id });
    }

    [HttpPost("formulaires/{id}/activer")]
    [Authorize(Roles = RolesApp.Admin + "," + RolesApp.ResponsableDI + "," + RolesApp.ResponsableQualite + "," + RolesApp.SuperviseurQualite)]
    public async Task<IActionResult> ActiverFormulaire(Guid id)
    {
        var result = await _formulaireService.ActiverFormulaireAsync(id);
        if (!result) return NotFound(new { success = false, message = "Formulaire introuvable ou n'est pas en statut BROUILLON." });
        return Ok(new { success = true, message = "Formulaire activé avec succès." });
    }

    [HttpPost("periodicites")]
    [Authorize(Roles = RolesApp.Admin + "," + RolesApp.ResponsableDI + "," + RolesApp.ResponsableQualite + "," + RolesApp.SuperviseurQualite)]
    public async Task<IActionResult> CreatePeriodicite([FromBody] CreatePeriodiciteDto request)
    {
        var result = await _referentielService.CreatePeriodiciteAsync(request);
        return Ok(new { success = true, data = result, id = result, periodiciteId = result });
    }
}
