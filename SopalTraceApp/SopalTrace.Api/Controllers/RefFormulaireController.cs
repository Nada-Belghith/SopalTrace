using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.DTOs.QualityPlans.Referentiels;
using SopalTrace.Application.Interfaces;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Api.Controllers;

[Route("api/referentiels/modeles-generiques")]
[ApiController]
public class RefFormulaireController : ControllerBase
{
    private readonly IRefFormulaireService _service;

    public RefFormulaireController(IRefFormulaireService service)
    {
        _service = service;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var data = await _service.GetByIdAsync(id);
        return Ok(new { success = true, data });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRefFormulaireDto dto)
    {
        var updated = await _service.UpdateConfigurationAsync(id, dto);
        if (!updated) return NotFound(new { success = false, message = "Formulaire introuvable." });
        return Ok(new { success = true, message = "Formulaire mis à jour." });
    }

    [HttpPost("nouvelle-version")]
    public async Task<IActionResult> NouvelleVersion([FromBody] NouvelleVersionRefFormulaireDto dto)
    {
        var newId = await _service.NouvelleVersionAsync(dto);
        var data = await _service.GetByIdAsync(newId);
        return Ok(new { success = true, data, message = "Nouvelle version créée et activée." });
    }
}
