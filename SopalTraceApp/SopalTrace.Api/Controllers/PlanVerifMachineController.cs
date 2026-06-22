using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.DTOs.QualityPlans.PlanVerifMachines;
using SopalTrace.Application.Interfaces;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlanVerifMachineController : ControllerBase
{
    private readonly IPlanVerifMachineService _planVerifMachineService;

    public PlanVerifMachineController(IPlanVerifMachineService planVerifMachineService)
    {
        _planVerifMachineService = planVerifMachineService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePlanVerifMachineRequestDto request)
    {
        var id = await _planVerifMachineService.CreerPlanVerifMachineAsync(request);
        return Ok(new { id = id });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var plan = await _planVerifMachineService.GetPlanVerifMachineByIdAsync(id);
        return Ok(plan);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? machineCode = null)
    {
        if (!string.IsNullOrEmpty(machineCode))
        {
            var plans = await _planVerifMachineService.GetPlansByMachineCodeAsync(machineCode);
            return Ok(plans);
        }
        else
        {
            var plans = await _planVerifMachineService.GetAllPlansAsync();
            return Ok(plans);
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePlanVerifMachineRequestDto request)
    {
        await _planVerifMachineService.MettreAJourPlanVerifMachineAsync(id, request);
        return NoContent();
    }
}
