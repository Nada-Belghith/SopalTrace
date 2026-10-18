using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.DTOs.DocumentsExternes;
using SopalTrace.Application.Services;
using SopalTrace.Domain.Constants;

namespace SopalTrace.Api.Controllers;

/// <summary>
/// Documents externes : chaque service (achats, commercial, qualité, développement industriel) dépose ses documents avec les numéros exigés par
/// leur type ; le nom normalisé est formé par la base. Le responsable qualité les consulte depuis la traçabilité par OF.
/// </summary>
[Authorize]
[Route("api/documents-externes")]
[ApiController]
public class DocumentsExternesController : ControllerBase
{
    private const string RolesDepot = RolesApp.ServiceAchats + "," + RolesApp.ServiceCommercial + "," + RolesApp.ServiceQualite + "," + RolesApp.ServiceDI;
    private const string RolesConsultation = RolesDepot + "," + RolesApp.ResponsableQualite;
    // Le service vérifie la taille configurée ; cette limite protège seulement le serveur d'un envoi démesuré.
    private const long TailleMaximaleEnvoi = 30 * 1024 * 1024;

    private readonly IDocumentExterneService _documents;

    public DocumentsExternesController(IDocumentExterneService documents) => _documents = documents;

    private string Role => User.FindFirstValue(ClaimTypes.Role) ?? "";

    /// <summary>Types de documents que l'utilisateur dépose, avec les numéros à saisir.</summary>
    [Authorize(Roles = RolesDepot)]
    [HttpGet("types")]
    public async Task<IActionResult> GetTypes() => Ok(new { success = true, data = await _documents.GetTypesAsync(Role) });

    /// <summary>Dépôt d'un document (formulaire multipart : numéros, fichier PDF ; remplacement avec motif).</summary>
    [Authorize(Roles = RolesDepot)]
    [HttpPost]
    [RequestSizeLimit(TailleMaximaleEnvoi)]
    public async Task<IActionResult> Deposer(IFormFile? fichier, [FromForm] string typeCode, [FromForm] string? numeroRapport, [FromForm] string? codeArticle,
        [FromForm] string? numeroBl, [FromForm] string? numeroOf, [FromForm] string? numeroCommandeFournisseur, [FromForm] string? numeroCommande, [FromForm] string? referenceCommandeClient,
        [FromForm] bool remplacer, [FromForm] string? motifRemplacement)
    {
        await using var flux = new MemoryStream();
        if (fichier != null) await fichier.CopyToAsync(flux);
        var document = await _documents.DeposerAsync(new DepotDocumentExterneRequest
        {
            TypeCode = typeCode ?? "",
            NumeroRapport = numeroRapport,
            CodeArticle = codeArticle,
            NumeroBl = numeroBl,
            NumeroOf = numeroOf,
            NumeroCommandeFournisseur = numeroCommandeFournisseur,
            NumeroCommande = numeroCommande,
            ReferenceCommandeClient = referenceCommandeClient,
            Remplacer = remplacer,
            MotifRemplacement = motifRemplacement,
            NomFichierOrigine = fichier?.FileName ?? "",
            Contenu = flux.ToArray()
        }, Role);
        var message = document.RemplaceDocumentId == null
            ? $"Document déposé sous le nom « {document.NomFichier} »."
            : $"Document remplacé : « {document.NomFichier} » est la nouvelle version, l'ancienne reste consultable.";
        return Ok(new { success = true, message, data = document });
    }

    /// <summary>Commande client Sage et sa référence client : aperçu du nom d'un BL avant son dépôt.</summary>
    [Authorize(Roles = RolesDepot)]
    [HttpGet("commandes-clients/{numeroCommande}")]
    public async Task<IActionResult> GetCommandeClient(string numeroCommande) =>
        Ok(new { success = true, data = await _documents.GetCommandeClientAsync(numeroCommande) });

    /// <summary>Documents déposés (les plus récents d'abord) : ceux du service de l'utilisateur, ou tous pour le responsable qualité.</summary>
    [Authorize(Roles = RolesConsultation)]
    [HttpGet]
    public async Task<IActionResult> Lister([FromQuery] string? type, [FromQuery] string? recherche) =>
        Ok(new { success = true, data = await _documents.ListerAsync(Role, type, recherche) });

    /// <summary>Fichier PDF d'un document, ouvert dans le navigateur.</summary>
    [Authorize(Roles = RolesConsultation)]
    [HttpGet("{id:guid}/fichier")]
    public async Task<IActionResult> Fichier(Guid id)
    {
        var fichier = await _documents.GetFichierAsync(id, Role);
        Response.Headers.ContentDisposition = new System.Net.Mime.ContentDisposition { Inline = true, FileName = fichier.NomFichier }.ToString();
        return File(fichier.Contenu, fichier.TypeContenu);
    }
}
