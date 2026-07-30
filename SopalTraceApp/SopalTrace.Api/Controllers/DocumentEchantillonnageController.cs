using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.DTOs;
using SopalTrace.Application.DTOs.QualityPlans.Echantillonnage;
using SopalTrace.Application.Interfaces;
using System;
using System.Threading.Tasks;

using SopalTrace.Domain.Constants;
using Microsoft.AspNetCore.Authorization;

namespace SopalTrace.Api.Controllers;

[Authorize(Roles = RolesApp.Admin + "," + RolesApp.ResponsableDI + "," + RolesApp.ResponsableQualite + "," + RolesApp.SuperviseurQualite)]
[ApiController]
[Route("api/documents-echantillonnage")]
public class DocumentEchantillonnageController : ControllerBase
{
    private readonly IDocumentEchantillonnageService _service;

    public DocumentEchantillonnageController(IDocumentEchantillonnageService service)
    {
        _service = service;
    }

    [HttpGet("actif")]
    public async Task<IActionResult> GetActiveDocument()
    {
        var plan = await _service.GetActiveDocumentAsync();
        return Ok(new { success = true, data = plan, message = plan == null ? "Aucun document actif trouvé." : "Document actif récupéré avec succès." });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDocumentById(Guid id)
    {
        var plan = await _service.GetDocumentByIdAsync(id);
        if (plan == null) return NotFound(new { success = false, message = "Document introuvable." });
        return Ok(new { success = true, data = plan, message = "Document récupéré avec succès." });
    }

    [HttpPost]
    public async Task<IActionResult> CreateDocument([FromBody] CreateDocumentEchantillonnageRequestDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var creePar = "Admin"; // TODO: Utiliser ICurrentUserService si disponible
        var newId = await _service.CreateDocumentAsync(request, creePar);

        return Ok(new { success = true, data = newId, message = "Document créé avec succès." });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDocument(Guid id, [FromBody] UpdateDocumentEchantillonnageRequestDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        await _service.UpdateDocumentAsync(id, request);
        return Ok(new { success = true, data = id, message = "Document mis à jour avec succès." });
    }

    [HttpPut("{id}/activer")]
    public async Task<IActionResult> ActivateDocument(Guid id)
    {
        var modifiePar = "Admin"; // TODO: Utiliser ICurrentUserService si disponible
        await _service.ActivateDocumentAsync(id, modifiePar);
        return Ok(new { success = true, data = id, message = "Document activé avec succès." });
    }

    [HttpPost("nouvelle-version")]
    public async Task<IActionResult> CreateNewVersion([FromBody] CreateNewVersionDocumentEchantillonnageRequestDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var newId = await _service.CreateNewVersionAsync(request);
        return Ok(new { success = true, data = newId, message = "Nouvelle version créée avec succès." });
    }
}
