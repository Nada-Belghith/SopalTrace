using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.DTOs;
using SopalTrace.Application.DTOs.QualityPlans.Echantillonnage;
using SopalTrace.Application.Interfaces;
using System;
using System.Threading.Tasks;

using SopalTrace.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using FluentValidation;
using SopalTrace.Application.Utilities;

namespace SopalTrace.Api.Controllers;

[Authorize(Roles = RolesApp.Admin + "," + RolesApp.ResponsableDI + "," + RolesApp.ResponsableQualite + "," + RolesApp.SuperviseurQualite)]
[ApiController]
[Route("api/documents-echantillonnage")]
public class DocumentEchantillonnageController : ControllerBase
{
    private readonly IDocumentEchantillonnageService _service;
    private readonly IValidator<CreateDocumentEchantillonnageRequestDto> _validateurCreation;
    private readonly IValidator<UpdateDocumentEchantillonnageRequestDto> _validateurModification;

    public DocumentEchantillonnageController(
        IDocumentEchantillonnageService service,
        IValidator<CreateDocumentEchantillonnageRequestDto> validateurCreation,
        IValidator<UpdateDocumentEchantillonnageRequestDto> validateurModification)
    {
        _service = service;
        _validateurCreation = validateurCreation;
        _validateurModification = validateurModification;
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
        await ValidationHelper.ValidateAndThrowAsync(_validateurCreation, request, "création du plan d'échantillonnage");

        var newId = await _service.CreateDocumentAsync(request);

        return Ok(new { success = true, data = newId, message = "Document créé avec succès." });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDocument(Guid id, [FromBody] UpdateDocumentEchantillonnageRequestDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        await ValidationHelper.ValidateAndThrowAsync(_validateurModification, request, "modification du plan d'échantillonnage");

        var rowVersion = await _service.UpdateDocumentAsync(id, request);
        return Ok(new { success = true, data = id, rowVersion, message = "Document mis à jour avec succès." });
    }

    [HttpPut("{id}/activer")]
    public async Task<IActionResult> ActivateDocument(Guid id)
    {
        await _service.ActivateDocumentAsync(id);
        return Ok(new { success = true, data = id, message = "Document activé avec succès." });
    }

    [HttpPost("nouvelle-version")]
    public async Task<IActionResult> CreateNewVersion([FromBody] CreateNewVersionDocumentEchantillonnageRequestDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        await ValidationHelper.ValidateAndThrowAsync(_validateurModification, request.Donnees, "nouvelle version du plan d'échantillonnage");

        var newId = await _service.CreateNewVersionAsync(request);
        return Ok(new { success = true, data = newId, message = "Nouvelle version créée avec succès." });
    }
}
