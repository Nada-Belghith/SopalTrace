using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.DTOs.QualityPlans.Documents;
using SopalTrace.Application.Interfaces;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Api.Controllers;

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

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var document = await _documentService.GetDocumentByIdAsync(id);
        if (document != null) return Ok(document);

        return NotFound(new { message = "Document introuvable." });
    }

    [HttpGet]
    public async Task<IActionResult> GetByFilters(
        [FromQuery] string typeDocumentCode,
        [FromQuery] string? natureComposantCode = null, 
        [FromQuery] string? operationCode = null, 
        [FromQuery] string? posteCode = null, 
        [FromQuery] string? familleProduitCode = null,
        [FromQuery] string? statut = null)
    {
        if (string.IsNullOrWhiteSpace(typeDocumentCode))
        {
            return BadRequest(new { message = "Le paramètre typeDocumentCode est requis." });
        }

        var documents = await _documentService.GetDocumentsByFiltersAsync(
            typeDocumentCode, natureComposantCode, operationCode, posteCode, familleProduitCode, statut);
            
        return Ok(documents);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDocumentRequestDto request)
    {
        var docId = await _documentService.CreerDocumentAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = docId }, new { id = docId });
    }

    [HttpPost("{id:guid}/version")]
    public async Task<IActionResult> CreateNewVersion(Guid id, [FromBody] NouvelleVersionDocumentRequestDto request)
    {
        if (id != request.AncienId)
            return BadRequest(new { message = "L'ID du document ne correspond pas." });

        var newDocId = await _documentService.CreerNouvelleVersionDocumentAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = newDocId }, new { id = newDocId });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateDocument(Guid id, [FromBody] UpdateDocumentRequestDto request)
    {
        var success = await _documentService.MettreAJourDocumentAsync(id, request);
        if (!success)
            return NotFound(new { message = "Document introuvable ou vous n'avez pas les droits." });

        return NoContent();
    }

    [HttpPost("{id:guid}/restaurer")]
    public async Task<IActionResult> Restaurer(Guid id, [FromBody] RestaurerDocumentRequestDto request)
    {
        if (id != request.DocumentArchiveId)
            return BadRequest(new { message = "L'ID du document ne correspond pas." });

        var newId = await _documentService.RestaurerDocumentArchiveAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = newId }, new { id = newId });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _documentService.SupprimerDocumentAsync(id);
        if (!success)
            return NotFound(new { message = "Document introuvable ou déjà supprimé." });
            
        return NoContent();
    }
}
