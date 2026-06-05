using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SopalTrace.Application.DTOs.QualityPlans.VerifMachine;
using SopalTrace.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Api.Controllers;

/// <summary>
/// Controller pour les plans de vérification machine (BEE, MAS, SER...).
/// Route principale : /api/plans-verif-machine
/// </summary>
[ApiController]
[Route("api/plans-verif-machine")]
public class PlanVerifMachineController : ControllerBase
{
    private readonly IPlanMachineFactory _planFactory;
    private readonly IValidator<CreateVerifMachineModeleDto> _validator;

    public PlanVerifMachineController(IPlanMachineFactory planFactory, IValidator<CreateVerifMachineModeleDto> validator)
    {
        _planFactory = planFactory;
        _validator = validator;
    }

    // =========================================================================
    // POST /api/plans-verif-machine
    // Création unifiée : reçoit l'ensemble du payload (flags + familles + lignes)
    // =========================================================================
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVerifMachineModeleDto request)
    {
        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(new { success = false, message = "Données invalides.", details = errors });
        }

        try
        {
            var service = _planFactory.GetService(request.MachineCode);
            var newId = await service.CreerPlanVerifAsync(request, "ADMIN");
            var data = await service.GetPlanVerifByIdAsync(newId);
            return Ok(new { success = true, planId = newId, version = data?.Version ?? 0, message = "Plan de vérification machine créé avec succès." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // =========================================================================
    // GET /api/plans-verif-machine/{id}
    // =========================================================================
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        try
        {
            var service = _planFactory.GetService("DEFAULT");
            var data = await service.GetPlanVerifByIdAsync(id);
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
    }

    // =========================================================================
    // PUT /api/plans-verif-machine/{id}
    // Mise à jour complète (remplace tout l'arbre)
    // =========================================================================
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateVerifMachineModeleDto request)
    {
        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(new { success = false, message = "Données invalides.", details = errors });
        }

        try
        {
            var service = _planFactory.GetService(request.MachineCode);
            var newId = await service.MettreAJourPlanVerifAsync(id, request, "ADMIN");
            return Ok(new { success = true, planId = newId, message = "Nouvelle version du plan créée et l'ancienne a été archivée." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // =========================================================================
    // POST /api/plans-verif-machine/{id}/nouvelle-version
    // =========================================================================
    [HttpPost("{id:guid}/nouvelle-version")]
    public async Task<IActionResult> NouvelleVersion(Guid id, [FromBody] NouvelleVersionVerifMachineDto request)
    {
        try
        {
            var service = _planFactory.GetService("DEFAULT"); // Could need machineCode if request has it
            var newId = await service.CreerNouvelleVersionAsync(request with { AncienId = id });
            var data = await service.GetPlanVerifByIdAsync(newId);
            return Ok(new { success = true, planId = newId, version = data?.Version ?? 0, message = "Nouvelle version créée." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // =========================================================================
    // PUT /api/plans-verif-machine/{id}/statut
    // =========================================================================
    [HttpPut("{id:guid}/statut")]
    public async Task<IActionResult> ChangerStatut(Guid id, [FromQuery] string statut)
    {
        try
        {
            if (statut == "ARCHIVE")
            {
                var service = _planFactory.GetService("DEFAULT");
                await service.ArchiverPlanAsync(id, "ADMIN"); // TODO: récupérer depuis JWT
                return Ok(new { success = true, message = "Plan archivé." });
            }
            return BadRequest(new { success = false, message = "Action non supportée." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // =========================================================================
    // POST /api/plans-verif-machine/restaurer
    // =========================================================================
    [HttpPost("restaurer")]
    public async Task<IActionResult> Restaurer([FromBody] NouvelleVersionVerifMachineDto request)
    {
        try
        {
            var service = _planFactory.GetService("DEFAULT");
            var newId = await service.RestaurerPlanAsync(request.AncienId, request.ModifiePar ?? "ADMIN", request.MotifModification);
            var data = await service.GetPlanVerifByIdAsync(newId);
            return Ok(new { success = true, planId = newId, version = data?.Version ?? 0, message = "Plan restauré." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // =========================================================================
    // GET /api/plans-verif-machine
    // Récupère tous les plans
    // =========================================================================
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var service = _planFactory.GetService("DEFAULT");
            var data = await service.GetTousLesPlansVerifAsync();
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // =========================================================================
    // ROUTES LEGACY
    // =========================================================================
    [HttpPut("{id:guid}/valeurs")]
    public async Task<IActionResult> UpdateFullTree(Guid id, [FromBody] List<VerifMachineLigneEditDto> lignes)
    {
        var service = _planFactory.GetService("DEFAULT");
        var ok = await service.MettreAJourValeursPlanAsync(id, lignes);
        if (!ok) return NotFound(new { success = false, message = "Plan introuvable." });
        return Ok(new { success = true, message = "Arbre synchronisé." });
    }

    [HttpPost("import-excel")]
    public async Task<IActionResult> ImportExcel(IFormFile file, [FromForm] string? configurationColonnesJson, [FromServices] IExcelImportFactory excelFactory)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { success = false, message = "Veuillez sélectionner un fichier." });

        try
        {
            var fileNameMatch = System.Text.RegularExpressions.Regex.Match(file.FileName, @"VM_([A-Z0-9-]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            string machineCode = fileNameMatch.Success ? fileNameMatch.Groups[1].Value : "";

            var importer = excelFactory.GetImporter(machineCode);

            using var stream = file.OpenReadStream();
            var result = await importer.ParseVerifMachineExcelAsync(stream, file.FileName, configurationColonnesJson);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Erreur lors de l'import : {ex.Message}" });
        }
    }

    // =========================================================================
    // GET /api/plans-verif-machine/machine/{machineCode}/familles
    // Retourne les familles de corps configurées pour une machine dans Machine_FamilleCorps
    // =========================================================================
    [HttpGet("machine/{machineCode}/familles")]
    public async Task<IActionResult> GetFamillesParMachine(string machineCode)
    {
        try
        {
            var service = _planFactory.GetService(machineCode);
            var familles = await service.GetFamillesParMachineAsync(machineCode);
            return Ok(new { success = true, data = familles });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
