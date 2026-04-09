using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.DTOs.QualityPlans.Assemblage;
using SopalTrace.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Api.Controllers;

[ApiController]
[Route("api/plans-assemblage")]
public class PlanAssemblageController : ControllerBase
{
    private readonly IPlanAssService _service;

    public PlanAssemblageController(IPlanAssService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePlanAssRequestDto req)
    {
        var id = await _service.CreerPlanAsync(req, "ADMIN");
        return Ok(new { success = true, id });
    }

    [HttpPut("{id}/valeurs")]
    public async Task<IActionResult> UpdateFullTree(Guid id, [FromBody] List<SectionAssEditDto> sections)
    {
        var ok = await _service.MettreAJourValeursPlanAsync(id, sections);
        if (!ok) return NotFound();
        return Ok(new { success = true, message = "Plan synchronisé et activé." });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var plan = await _service.GetPlanByIdAsync(id);
        return Ok(plan);
    }
}