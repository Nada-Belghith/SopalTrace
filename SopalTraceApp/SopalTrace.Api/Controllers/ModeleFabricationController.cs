using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.DTOs.QualityPlans.Modeles;
using SopalTrace.Application.Interfaces;
using System;
using System.Threading.Tasks;

using SopalTrace.Domain.Constants;

namespace SopalTrace.Api.Controllers;

/// <summary>Modèles de fabrication. Les erreurs sont journalisées et traduites par ExceptionMiddleware.</summary>
[Authorize(Roles = RolesApp.Admin + "," + RolesApp.ResponsableDI)]
[ApiController]
[Route("api/[controller]")]
public class ModeleFabricationController : ControllerBase
{
    private readonly IModeleFabricationService _modeleService;

    public ModeleFabricationController(IModeleFabricationService modeleService)
    {
        _modeleService = modeleService;
    }

    [HttpGet]
    public async Task<IActionResult> GetByFilters(
        [FromQuery] string? natureComposantCode = null,
        [FromQuery] string? operationCode = null,
        [FromQuery] string? familleProduitCode = null,
        [FromQuery] string? statut = null)
    {
        return Ok(await _modeleService.GetModelsByFiltersAsync(natureComposantCode, operationCode, familleProduitCode, statut));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var modele = await _modeleService.GetModelByIdAsync(id);
        if (modele == null) return NotFound("Modèle non trouvé.");
        return Ok(modele);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateModeleRequestDto request)
    {
        var id = await _modeleService.CreateModelAsync(request);
        return Ok(new { id });
    }

    [HttpPost("nouvelle-version")]
    public async Task<IActionResult> CreateNewVersion([FromBody] NouvelleVersionModeleRequestDto request)
    {
        var id = await _modeleService.CreateNewVersionAsync(request);
        return Ok(new { id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateModeleRequestDto request)
    {
        var rowVersion = await _modeleService.UpdateModelAsync(id, request);
        if (rowVersion == null) return NotFound();
        return Ok(new { success = true, rowVersion });
    }
}
