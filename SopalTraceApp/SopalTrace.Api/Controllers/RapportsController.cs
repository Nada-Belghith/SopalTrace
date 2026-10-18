using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.DTOs.Rapports;
using SopalTrace.Application.Interfaces.Rapports;
using SopalTrace.Application.Services.Rapports;
using SopalTrace.Domain.Constants;

namespace SopalTrace.Api.Controllers;

/// <summary>
/// Modèles de rapport (fichiers Excel de Sopal), gérés par l'administrateur (le superviseur qualité suit leur état), et documents mis en forme à la
/// demande (PDF, impression, Excel du modèle), consultés par les rôles qualité.
/// </summary>
[Authorize]
[Route("api/rapports")]
[ApiController]
public class RapportsController : ControllerBase
{
    // Rôles cumulés si posés sur la classe et la méthode : chaque action porte donc ses propres rôles.
    // Le superviseur qualité définit les formulaires ; l'administrateur leur envoie et rattache les modèles Excel
    // (fichiers techniques à champs {{…}}), y compris lors d'un changement de logo. Les deux suivent l'état des modèles.
    private const string RolesGestion = RolesApp.Admin;
    private const string RolesSuivi = RolesApp.Admin + "," + RolesApp.SuperviseurQualite;
    private const string RolesConsultation = RolesApp.Admin + "," + RolesApp.ResponsableQualite + "," + RolesApp.SuperviseurQualite + "," + RolesApp.ResponsableDI;
    private const long TailleMaximaleEnvoi = 6 * 1024 * 1024;

    private readonly IModeleRapportService _modeles;
    private readonly IGenerationDocumentService _generation;

    public RapportsController(IModeleRapportService modeles, IGenerationDocumentService generation)
    {
        _modeles = modeles;
        _generation = generation;
    }

    /// <summary>Publication d'une version : motif facultatif ; formulaire visé ou version remplacée selon l'action.</summary>
    public sealed record PublicationRequest(string? Commentaire, Guid? FormulaireId, Guid? VersionRemplaceeId);
    public sealed record ChangementLogoRequest(List<Guid>? FormulaireIds, string? Commentaire);
    public sealed record DemandeModeleRequest(string? Motif, string? ChangementVersion);
    public sealed record TraitementDemandeRequest(Guid VersionId, string? Commentaire);

    // ── Modèles ──────────────────────────────────────────────────────────

    [Authorize(Roles = RolesGestion)]
    [HttpGet("modeles")]
    public async Task<IActionResult> GetModeles() => Ok(new { success = true, data = await _modeles.GetModelesAsync() });

    [Authorize(Roles = RolesSuivi)]
    [HttpGet("formulaires")]
    public async Task<IActionResult> GetFormulairesParRole() => Ok(new { success = true, data = await _modeles.GetFormulairesParRoleAsync() });

    [Authorize(Roles = RolesGestion)]
    [HttpGet("modeles/{code}/formulaires")]
    public async Task<IActionResult> GetFormulaires(string code) => Ok(new { success = true, data = await _modeles.GetFormulairesAsync(code) });

    [Authorize(Roles = RolesGestion)]
    [HttpGet("modeles/{code}/champs")]
    public async Task<IActionResult> GetChamps(string code, [FromQuery] Guid? formulaireId) =>
        Ok(new { success = true, data = await _modeles.GetChampsAsync(code, formulaireId) });

    /// <summary>Envoie un nouveau fichier : enregistré en brouillon s'il est accepté, sinon refusé avec la liste des erreurs.</summary>
    [Authorize(Roles = RolesGestion)]
    [HttpPost("modeles/{code}/versions")]
    [RequestSizeLimit(TailleMaximaleEnvoi)]
    public async Task<IActionResult> EnvoyerVersion(string code, IFormFile? fichier, [FromForm] string? commentaire, [FromForm] Guid? formulaireId)
    {
        if (fichier == null || fichier.Length == 0)
            return BadRequest(new { success = false, message = "Aucun fichier reçu.", erreurs = new[] { "Aucun fichier reçu." } });

        await using var flux = new MemoryStream();
        await fichier.CopyToAsync(flux);
        var verification = await _modeles.EnvoyerVersionAsync(code, fichier.FileName, flux.ToArray(), commentaire, formulaireId);
        if (!verification.Accepte)
            return BadRequest(new { success = false, message = "Fichier refusé : corrigez les points signalés.", erreurs = verification.Erreurs, verification.Avertissements, verification.Champs });
        var message = verification.Version!.Statut == StatutsVersionModele.Brouillon
            ? $"Version {verification.Version.Numero} enregistrée en brouillon : vérifiez l'aperçu puis publiez-la."
            : $"Fichier déjà envoyé : la version {verification.Version.Numero} est reprise.";
        return Ok(new { success = true, message, data = verification });
    }

    [Authorize(Roles = RolesGestion)]
    [HttpGet("modeles/versions/{id:guid}/fichier")]
    public async Task<IActionResult> TelechargerVersion(Guid id) => Fichier(await _modeles.TelechargerAsync(id));

    [Authorize(Roles = RolesGestion)]
    [HttpGet("modeles/versions/{id:guid}/apercu")]
    public async Task<IActionResult> Apercu(Guid id, [FromQuery] Guid? formulaireId) =>
        Fichier(await _modeles.ApercuAsync(id, formulaireId), enLigne: true);

    /// <summary>La version devient le modèle par défaut de son type (documents créés à partir de maintenant).</summary>
    [Authorize(Roles = RolesGestion)]
    [HttpPost("modeles/versions/{id:guid}/par-defaut")]
    public async Task<IActionResult> PublierParDefaut(Guid id, [FromBody] PublicationRequest? requete)
    {
        var publication = await _modeles.PublierParDefautAsync(id, requete?.Commentaire);
        return Ok(new { success = true, message = $"Version {publication.Version.Numero} : modèle par défaut des documents créés à partir de maintenant.", data = publication });
    }

    /// <summary>Modèle conçu pour une version de formulaire (nouvelle structure), y compris pour ses documents déjà créés.</summary>
    [Authorize(Roles = RolesGestion)]
    [HttpPost("modeles/versions/{id:guid}/formulaire")]
    public async Task<IActionResult> AffecterAuFormulaire(Guid id, [FromBody] PublicationRequest requete)
    {
        if (requete.FormulaireId is not { } formulaireId)
            return BadRequest(new { success = false, message = "Choisissez la version de formulaire." });
        var publication = await _modeles.AffecterAuFormulaireAsync(id, formulaireId, requete.Commentaire);
        return Ok(new { success = true, message = $"Version {publication.Version.Numero} : modèle du formulaire {string.Join(", ", publication.Formulaires)}.", data = publication });
    }

    /// <summary>Révision (logo…) d'une version : pour les documents créés à partir de maintenant, partout où elle servait.</summary>
    [Authorize(Roles = RolesGestion)]
    [HttpPost("modeles/versions/{id:guid}/revision")]
    public async Task<IActionResult> Reviser(Guid id, [FromBody] PublicationRequest requete)
    {
        if (requete.VersionRemplaceeId is not { } remplacee)
            return BadRequest(new { success = false, message = "Choisissez la version révisée." });
        var publication = await _modeles.ReviserAsync(id, remplacee, requete.Commentaire);
        var portee = string.Join(", ", publication.Formulaires);
        if (publication.ParDefaut) portee = string.IsNullOrEmpty(portee) ? "modèle par défaut" : portee + " et modèle par défaut";
        return Ok(new { success = true, message = $"Révision {publication.Version.Numero} publiée ({portee}) : documents créés à partir de maintenant.", data = publication });
    }

    /// <summary>
    /// Nouveau logo pour les formulaires choisis : chaque formulaire utilisé passe à sa version suivante avec ce modèle,
    /// ses documents actifs sont archivés tels quels puis recréés en version suivante.
    /// </summary>
    [Authorize(Roles = RolesGestion)]
    [HttpPost("modeles/versions/{id:guid}/changement-logo")]
    public async Task<IActionResult> ChangerLogo(Guid id, [FromBody] ChangementLogoRequest requete)
    {
        var publication = await _modeles.ChangerLogoAsync(id, requete.FormulaireIds ?? new List<Guid>(), requete.Commentaire);
        return Ok(new { success = true, data = publication,
            message = $"Nouveau logo (modèle V{publication.Version.Numero}) : {string.Join(" ; ", publication.Formulaires)}." });
    }

    // ── Demandes de modèle ───────────────────────────────────────────────

    /// <summary>Demandes de modèle (toutes, ou d'un état : PENDING, DONE, CANCELLED).</summary>
    [Authorize(Roles = RolesSuivi)]
    [HttpGet("demandes-modele")]
    public async Task<IActionResult> GetDemandes([FromQuery] string? statut) =>
        Ok(new { success = true, data = await _modeles.GetDemandesAsync(string.IsNullOrWhiteSpace(statut) ? null : statut.Trim().ToUpperInvariant()) });

    /// <summary>Superviseur qualité : demande un nouveau modèle (même version ou version suivante) pour une ligne de formulaire.</summary>
    [Authorize(Roles = RolesApp.SuperviseurQualite)]
    [HttpPost("formulaires/{id:guid}/demande-modele")]
    public async Task<IActionResult> DemanderModele(Guid id, [FromBody] DemandeModeleRequest requete)
    {
        var demande = await _modeles.DemanderModeleAsync(id, requete.Motif, requete.ChangementVersion);
        return Ok(new { success = true, data = demande, message = "Demande envoyée à l'administrateur." });
    }

    [Authorize(Roles = RolesApp.SuperviseurQualite)]
    [HttpPost("demandes-modele/{id:long}/annuler")]
    public async Task<IActionResult> AnnulerDemande(long id)
    {
        await _modeles.AnnulerDemandeAsync(id);
        return Ok(new { success = true, message = "Demande annulée." });
    }

    /// <summary>Administrateur : rattache le modèle qui satisfait la demande (le choix de version du superviseur s'applique).</summary>
    [Authorize(Roles = RolesGestion)]
    [HttpPost("demandes-modele/{id:long}/traiter")]
    public async Task<IActionResult> TraiterDemande(long id, [FromBody] TraitementDemandeRequest requete)
    {
        var publication = await _modeles.TraiterDemandeAsync(id, requete.VersionId, requete.Commentaire);
        return Ok(new { success = true, data = publication,
            message = $"Modèle M{publication.Version.Numero} rattaché : {string.Join(" ; ", publication.Formulaires)}." });
    }

    // ── Documents mis en forme ───────────────────────────────────────────

    [Authorize(Roles = RolesConsultation)]
    [HttpGet("documents")]
    public async Task<IActionResult> GetDocuments([FromQuery] string? categorie, [FromQuery] string? type, [FromQuery] string? recherche) =>
        Ok(new { success = true, data = await _generation.GetSourcesAsync(categorie, type, recherche) });

    /// <summary>Dossier d'un OF : ses documents internes et ses documents externes. Réservé au responsable qualité.</summary>
    [Authorize(Roles = RolesApp.ResponsableQualite)]
    [HttpGet("tracabilite/{numeroOf}")]
    public async Task<IActionResult> GetTracabilite(string numeroOf, [FromServices] SopalTrace.Application.Services.IDocumentExterneService externes)
    {
        var dossier = await _generation.GetTracabiliteOfAsync(numeroOf);
        dossier.Externes = await externes.GetDossierOfAsync(dossier.NumeroOf);
        return Ok(new { success = true, data = dossier });
    }

    [Authorize(Roles = RolesConsultation)]
    [HttpGet("documents/par-document/{documentId:guid}")]
    public async Task<IActionResult> GetEtat(Guid documentId) =>
        Ok(new { success = true, data = await _generation.GetEtatAsync(SourceRapport.Document(documentId)) });

    [Authorize(Roles = RolesConsultation)]
    [HttpGet("documents/par-execution/{executionId:guid}")]
    public async Task<IActionResult> GetEtatExecution(Guid executionId) =>
        Ok(new { success = true, data = await _generation.GetEtatAsync(SourceRapport.Execution(executionId)) });

    /// <summary>PDF du document avec son modèle (ou le modèle actuel), produit à la demande.</summary>
    /// <summary>Fiche vierge du document qualité (celle que rempliront les opérateurs), en PDF.</summary>
    [Authorize(Roles = RolesConsultation)]
    [HttpGet("documents/par-document/{documentId:guid}/fiche-vierge/pdf")]
    public async Task<IActionResult> FicheViergePdf(Guid documentId) => Fichier(await _generation.FicheViergeAsync(documentId, pdf: true), enLigne: true);

    /// <summary>Fiche vierge du document qualité, en Excel rempli.</summary>
    [Authorize(Roles = RolesConsultation)]
    [HttpGet("documents/par-document/{documentId:guid}/fiche-vierge/excel")]
    public async Task<IActionResult> FicheViergeExcel(Guid documentId) => Fichier(await _generation.FicheViergeAsync(documentId, pdf: false));

    [Authorize(Roles = RolesConsultation)]
    [HttpGet("documents/par-document/{documentId:guid}/pdf")]
    public async Task<IActionResult> PdfDocument(Guid documentId, [FromQuery] bool modeleActuel = false) =>
        Fichier(await _generation.ObtenirAsync(SourceRapport.Document(documentId), modeleActuel, pdf: true), enLigne: true);

    [Authorize(Roles = RolesConsultation)]
    [HttpGet("documents/par-execution/{executionId:guid}/pdf")]
    public async Task<IActionResult> PdfExecution(Guid executionId, [FromQuery] bool modeleActuel = false) =>
        Fichier(await _generation.ObtenirAsync(SourceRapport.Execution(executionId), modeleActuel, pdf: true), enLigne: true);

    /// <summary>Excel rempli du document.</summary>
    [Authorize(Roles = RolesConsultation)]
    [HttpGet("documents/par-document/{documentId:guid}/excel")]
    public async Task<IActionResult> ExcelDocument(Guid documentId, [FromQuery] bool modeleActuel = false) =>
        Fichier(await _generation.ObtenirAsync(SourceRapport.Document(documentId), modeleActuel, pdf: false));

    [Authorize(Roles = RolesConsultation)]
    [HttpGet("documents/par-execution/{executionId:guid}/excel")]
    public async Task<IActionResult> ExcelExecution(Guid executionId, [FromQuery] bool modeleActuel = false) =>
        Fichier(await _generation.ObtenirAsync(SourceRapport.Execution(executionId), modeleActuel, pdf: false));

    /// <summary>Fichier Excel du modèle qui met en forme le document (vide, avec ses champs).</summary>
    [Authorize(Roles = RolesSuivi)]
    [HttpGet("documents/par-document/{documentId:guid}/modele")]
    public async Task<IActionResult> ModeleDocument(Guid documentId, [FromQuery] bool modeleActuel = false) =>
        Fichier(await _generation.TelechargerModeleAsync(SourceRapport.Document(documentId), modeleActuel));

    [Authorize(Roles = RolesSuivi)]
    [HttpGet("documents/par-execution/{executionId:guid}/modele")]
    public async Task<IActionResult> ModeleExecution(Guid executionId, [FromQuery] bool modeleActuel = false) =>
        Fichier(await _generation.TelechargerModeleAsync(SourceRapport.Execution(executionId), modeleActuel));

    /// <summary>PDF ou Excel d'un document déjà produit (historique).</summary>
    [Authorize(Roles = RolesConsultation)]
    [HttpGet("documents/{id:guid}/pdf")]
    public async Task<IActionResult> TelechargerPdf(Guid id) => Fichier(await _generation.TelechargerAsync(id, pdf: true), enLigne: true);

    [Authorize(Roles = RolesConsultation)]
    [HttpGet("documents/{id:guid}/excel")]
    public async Task<IActionResult> TelechargerExcel(Guid id) => Fichier(await _generation.TelechargerAsync(id, pdf: false));

    private FileContentResult Fichier(FichierDto fichier, bool enLigne = false)
    {
        // Un PDF consulté s'ouvre dans le navigateur ; les autres fichiers sont téléchargés.
        if (enLigne)
        {
            Response.Headers.ContentDisposition = new System.Net.Mime.ContentDisposition { Inline = true, FileName = fichier.NomFichier }.ToString();
            return File(fichier.Contenu, fichier.TypeContenu);
        }
        return File(fichier.Contenu, fichier.TypeContenu, fichier.NomFichier);
    }
}
