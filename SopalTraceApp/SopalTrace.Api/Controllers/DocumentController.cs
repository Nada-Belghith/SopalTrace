using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.DTOs.QualityPlans.Documents;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Constants;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Api.Controllers;

/// <summary>
/// Gestion des documents de types centralisés : CTRL_POSTE, RESULTAT_CF, PLAN_ASS, PLAN_PF.
/// 
/// Droits d'accès :
/// - Lecture    : tous les utilisateurs authentifiés
/// - Écriture   : SuperviseurQualite uniquement
/// - Suppression: SuperviseurQualite uniquement
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DocumentController : ControllerBase
{
    private readonly IDocumentService _documentService;

    public DocumentController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    // ── Lecture (tous les utilisateurs connectés) ─────────────────────────────

    /// <summary>Récupère un document par son identifiant.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var document = await _documentService.GetDocumentByIdAsync(id);
        if (document != null) return Ok(document);
        return NotFound(new { message = "Document introuvable." });
    }

    /// <summary>Recherche des documents par filtres (type, nature, poste, famille, statut…).</summary>
    [HttpGet]
    public async Task<IActionResult> GetByFilters(
        [FromQuery] string  typeDocumentCode,
        [FromQuery] string? natureComposantCode  = null,
        [FromQuery] string? operationCode        = null,
        [FromQuery] string? posteCode            = null,
        [FromQuery] string? familleProduitCode   = null,
        [FromQuery] string? statut               = null)
    {
        if (string.IsNullOrWhiteSpace(typeDocumentCode))
            return BadRequest(new { message = "Le paramètre typeDocumentCode est requis." });

        var documents = await _documentService.GetDocumentsByFiltersAsync(
            typeDocumentCode, natureComposantCode, operationCode,
            posteCode, familleProduitCode, statut);

        return Ok(documents);
    }

    // ── Écriture (SuperviseurQualite uniquement) ──────────────────────────────

    /// <summary>
    /// Crée un nouveau document (V1, statut ACTIF).
    /// Si un document ACTIF existe déjà pour ce contexte, il est archivé automatiquement.
    /// </summary>
    [Authorize(Roles = RolesApp.SuperviseurQualite)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDocumentRequestDto request)
    {
        var docId = await _documentService.CreateDocumentAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = docId }, new { id = docId });
    }

    /// <summary>
    /// Crée une nouvelle version du document (V+1, statut ACTIF).
    /// L'ancien document est archivé automatiquement.
    /// </summary>
    [Authorize(Roles = RolesApp.SuperviseurQualite)]
    [HttpPost("{id:guid}/version")]
    public async Task<IActionResult> CreateNewVersion(
        Guid id,
        [FromBody] NouvelleVersionDocumentRequestDto request)
    {
        if (id != request.AncienId)
            return BadRequest(new { message = "L'ID du document ne correspond pas à AncienId." });

        var newDocId = await _documentService.CreateNewVersionAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = newDocId }, new { id = newDocId });
    }

    /// <summary>
    /// Correction mineure : modifie le document en place.
    /// Aucune incrémentation de version, aucun archivage.
    /// </summary>
    [Authorize(Roles = RolesApp.SuperviseurQualite)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateDocument(
        Guid id,
        [FromBody] UpdateDocumentRequestDto request)
    {
        var success = await _documentService.UpdateDocumentAsync(id, request);
        if (!success)
            return NotFound(new { message = "Document introuvable." });
        return NoContent();
    }
}
