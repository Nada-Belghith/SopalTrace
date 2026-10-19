using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.DTOs.QualityPlans.DocumentVerifMachines;
using SopalTrace.Application.Interfaces;
using System;
using System.Threading.Tasks;

using SopalTrace.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using FluentValidation;
using SopalTrace.Application.Utilities;

namespace SopalTrace.Api.Controllers;

[Authorize(Roles = RolesApp.Admin + "," + RolesApp.ResponsableDI + "," + RolesApp.ResponsableQualite + "," + RolesApp.SuperviseurQualite + "," + RolesApp.Operateur)]
[ApiController]
[Route("api/[controller]")]
public class DocumentVerifMachineController : ControllerBase
{
    private readonly IDocumentVerifMachineService _documentVerifMachineService;
    private readonly IValidator<CreateDocumentVerifMachineRequestDto> _validateur;

    public DocumentVerifMachineController(IDocumentVerifMachineService documentVerifMachineService,
        IValidator<CreateDocumentVerifMachineRequestDto> validateur)
    {
        _documentVerifMachineService = documentVerifMachineService;
        _validateur = validateur;
    }

    [HttpPost]
    // Écriture d'un document qualité : le superviseur qualité (et l'administrateur), comme les autres documents qualité.
    [Authorize(Roles = RolesApp.Admin + "," + RolesApp.SuperviseurQualite)]
    public async Task<IActionResult> Create([FromBody] CreateDocumentVerifMachineRequestDto request)
    {
        await ValidationHelper.ValidateAndThrowAsync(_validateur, request, "création du plan de vérification machine");
        var id = await _documentVerifMachineService.CreateDocumentAsync(request);
        return Ok(new { id = id });
    }

    [HttpPost("nouvelle-version")]
    // Écriture d'un document qualité : le superviseur qualité (et l'administrateur), comme les autres documents qualité.
    [Authorize(Roles = RolesApp.Admin + "," + RolesApp.SuperviseurQualite)]
    public async Task<IActionResult> CreateNouvelleVersion([FromBody] NouvelleVersionVerifMachineRequestDto request)
    {
        await ValidationHelper.ValidateAndThrowAsync(_validateur, request.Donnees, "nouvelle version du plan de vérification machine");
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
    // Écriture d'un document qualité : le superviseur qualité (et l'administrateur), comme les autres documents qualité.
    [Authorize(Roles = RolesApp.Admin + "," + RolesApp.SuperviseurQualite)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDocumentVerifMachineRequestDto request)
    {
        await ValidationHelper.ValidateAndThrowAsync<CreateDocumentVerifMachineRequestDto>(_validateur, request, "modification du plan de vérification machine");
        var rowVersion = await _documentVerifMachineService.UpdateDocumentAsync(id, request);
        return Ok(new { rowVersion });
    }
}
