using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.DTOs.QualityPlans.DocumentVerifMachines;
using SopalTrace.Application.Interfaces;
using System;
using System.Threading.Tasks;

using SopalTrace.Domain.Constants;
using Microsoft.AspNetCore.Authorization;

namespace SopalTrace.Api.Controllers;

[Authorize(Roles = RolesApp.Admin + "," + RolesApp.ResponsableDI + "," + RolesApp.ResponsableQualite + "," + RolesApp.SuperviseurQualite + "," + RolesApp.Operateur)]
[ApiController]
[Route("api/[controller]")]
public class DocumentVerifMachineController : ControllerBase
{
    private readonly IDocumentVerifMachineService _documentVerifMachineService;

    public DocumentVerifMachineController(IDocumentVerifMachineService documentVerifMachineService)
    {
        _documentVerifMachineService = documentVerifMachineService;
    }

    [HttpPost]
    [Authorize(Roles = RolesApp.Admin + "," + RolesApp.ResponsableDI + "," + RolesApp.ResponsableQualite + "," + RolesApp.SuperviseurQualite)]
    public async Task<IActionResult> Create([FromBody] CreateDocumentVerifMachineRequestDto request)
    {
        var id = await _documentVerifMachineService.CreateDocumentAsync(request);
        return Ok(new { id = id });
    }

    [HttpPost("nouvelle-version")]
    [Authorize(Roles = RolesApp.Admin + "," + RolesApp.ResponsableDI + "," + RolesApp.ResponsableQualite + "," + RolesApp.SuperviseurQualite)]
    public async Task<IActionResult> CreateNouvelleVersion([FromBody] NouvelleVersionVerifMachineRequestDto request)
    {
        var id = await _documentVerifMachineService.CreateNewVersionAsync(request);
        return Ok(new { id = id });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var plan = await _documentVerifMachineService.GetDocumentByIdAsync(id);
        return Ok(plan);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? machineCode = null)
    {
        if (!string.IsNullOrEmpty(machineCode))
        {
            var plans = await _documentVerifMachineService.GetDocumentsByMachineCodeAsync(machineCode);
            return Ok(plans);
        }
        else
        {
            var plans = await _documentVerifMachineService.GetAllDocumentsAsync();
            return Ok(plans);
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = RolesApp.Admin + "," + RolesApp.ResponsableDI + "," + RolesApp.ResponsableQualite + "," + RolesApp.SuperviseurQualite)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDocumentVerifMachineRequestDto request)
    {
        await _documentVerifMachineService.UpdateDocumentAsync(id, request);
        return NoContent();
    }
}
