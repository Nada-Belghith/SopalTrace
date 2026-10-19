using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.DTOs.QualityPlans.Referentiels;
using SopalTrace.Application.Interfaces;
using System;
using System.Threading.Tasks;

using SopalTrace.Domain.Constants;
using Microsoft.AspNetCore.Authorization;

namespace SopalTrace.Api.Controllers;

[Authorize(Roles = RolesApp.Admin + "," + RolesApp.ResponsableDI + "," + RolesApp.SuperviseurQualite)]
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

    [Authorize(Roles = RolesApp.Admin + "," + RolesApp.SuperviseurQualite)]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRefFormulaireDto dto)
    {
        var updated = await _service.UpdateConfigurationAsync(id, dto);
        if (!updated) return NotFound(new { success = false, message = "Formulaire introuvable." });
        return Ok(new { success = true, message = "Formulaire mis à jour." });
    }

    [Authorize(Roles = RolesApp.Admin + "," + RolesApp.SuperviseurQualite)]
    [HttpPost("nouvelle-version")]
    public async Task<IActionResult> NouvelleVersion([FromBody] NouvelleVersionRefFormulaireDto dto)
    {
        var newId = await _service.NouvelleVersionAsync(dto);
        var data = await _service.GetByIdAsync(newId);
        return Ok(new { success = true, data, message = "Nouvelle version créée et activée." });
    }
}
