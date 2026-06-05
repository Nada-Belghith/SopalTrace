using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.DTOs.QualityPlans.PlanFabrication;
using SopalTrace.Application.DTOs.QualityPlans.Referentiels;
using SopalTrace.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Api.Controllers;

[Route("api/plans-fabrication")]
[ApiController]
public class PlanFabricationController : ControllerBase
{
    private readonly IPlanFabricationService _planService;
    private readonly IReferentielService _referentielService;
    private readonly SopalTraceDbContext _context;

    public PlanFabricationController(IPlanFabricationService planService, IReferentielService referentielService, SopalTraceDbContext context)
    {
        _planService = planService;
        _referentielService = referentielService;
        _context = context;
    }

    // ⚠️ NOUVELLE ROUTE : Permet de renvoyer la liste des plans actifs pour l'UI de clonage
    [HttpGet("liste")]
    public async Task<IActionResult> GetPlansByFilters(
        [FromQuery] string? typeRobinet, 
        [FromQuery] string? natureComposant, 
        [FromQuery] string? operation,
        [FromQuery] string? poste)
    {
        var data = await _planService.GetPlansByFiltersAsync(typeRobinet, natureComposant, operation, poste);
        return Ok(new { success = true, data });
    }

    [HttpPost("instancier")]
    public async Task<IActionResult> InstancierPlan([FromBody] CreatePlanRequestDto request)
    {
        var id = await _planService.InstancierPlanDepuisModeleAsync(request);
        return Ok(new { success = true, planId = id, message = "Plan initialisé avec succès." });
    }

    private bool IsAssemblage(string? operationCode, string? natureComposantCode, string? typeRobinetCode)
    {
        if (operationCode == "ASS") return true;
        var nat = natureComposantCode?.Trim().ToUpper();
        if (nat == "PISTON" || nat == "PF") return true;
        var type = typeRobinetCode?.Trim().ToUpper();
        if (type == "PISTON") return true;
        return false;
    }

    // ⚠️ NOUVELLE ROUTE : Vérifie l'état complet (Brouillon ET Actif)
    [HttpGet("verifier-etat")]
    public async Task<IActionResult> VerifierEtatPlan(
        [FromQuery] string articleCode,
        [FromQuery] Guid? modeleId,
        [FromQuery] string? typeRobinetCode,
        [FromQuery] string? natureComposantCode,
        [FromQuery] string? operationCode = null,
        [FromQuery] string? posteCode = null) // <-- Prise en charge posteCode!
    {
        bool isAss = IsAssemblage(operationCode, natureComposantCode, typeRobinetCode);
        
        string? opCode = operationCode;
        if (string.IsNullOrEmpty(opCode) && modeleId.HasValue && modeleId.Value != Guid.Empty)
        {
            if (isAss)
            {
                opCode = await _context.PlanAssemblageEntetes
                    .Where(m => m.Id == modeleId.Value)
                    .Select(m => m.OperationCode)
                    .FirstOrDefaultAsync();
            }
            else
            {
                opCode = await _context.ModeleFabricationEntetes
                    .Where(m => m.Id == modeleId.Value)
                    .Select(m => m.OperationCode)
                    .FirstOrDefaultAsync();
            }
        }

        if (isAss)
        {
            // PlanAssemblageEntete n'a pas de CodeArticleSage : c'est un modèle générique filtré par opération/famille
            var planQuery = _context.PlanAssemblageEntetes.AsQueryable();

            var brouillonQuery = planQuery.Where(p => p.Statut == "BROUILLON");
            if (!string.IsNullOrWhiteSpace(typeRobinetCode)) brouillonQuery = brouillonQuery.Where(p => p.FamilleProduitFiniCode == typeRobinetCode);
            if (!string.IsNullOrWhiteSpace(natureComposantCode)) brouillonQuery = brouillonQuery.Where(p => p.NatureArticleCode == natureComposantCode);
            if (!string.IsNullOrWhiteSpace(opCode)) brouillonQuery = brouillonQuery.Where(p => p.OperationCode == opCode);
            if (!string.IsNullOrWhiteSpace(posteCode)) brouillonQuery = brouillonQuery.Where(p => p.PosteCode == posteCode);

            var actifQuery = planQuery.Where(p => p.Statut == "ACTIF");
            if (!string.IsNullOrWhiteSpace(opCode)) actifQuery = actifQuery.Where(p => p.OperationCode == opCode);
            if (!string.IsNullOrWhiteSpace(posteCode)) actifQuery = actifQuery.Where(p => p.PosteCode == posteCode);

            var brouillon = await brouillonQuery.OrderByDescending(p => p.Version).Select(p => p.Id).FirstOrDefaultAsync();
            var actif = await actifQuery.Select(p => (int?)p.Version).FirstOrDefaultAsync();

            return Ok(new { success = true, hasBrouillon = brouillon != Guid.Empty, brouillonId = brouillon, hasActif = actif.HasValue, actifVersion = actif ?? 0 });
        }
        else
        {
            var planQuery = _context.PlanFabricationEntetes.Where(p => p.CodeArticleSage == articleCode);



        var brouillonQuery = planQuery
            .Where(p => p.Statut == "BROUILLON")
            .Include(p => p.ModeleSource)
            .AsQueryable();

        // TypeRobinetCode supprimé du modèle de fabrication

        if (!string.IsNullOrWhiteSpace(natureComposantCode))
        {
            brouillonQuery = brouillonQuery.Where(p =>
                p.ModeleSourceId == null || p.ModeleSource!.NatureArticleCode == natureComposantCode);
        }
        
        // <-- NOUVEAU: Applique le contrôle d'Opération sur le Brouillon également
        if (!string.IsNullOrWhiteSpace(opCode))
        {
            brouillonQuery = brouillonQuery.Where(p => 
                p.OperationCode == opCode || 
                (p.ModeleSourceId != null && p.ModeleSource!.OperationCode == opCode));
        }

        // PosteCode supprimé pour la fabrication

        var actifQuery = planQuery
            .Where(p => p.Statut == "ACTIF")
            .Include(p => p.ModeleSource)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(opCode))
        {
            // Tolérer les Plans avec Source Nulle (Vierge) ou correspondants à l'opération
            actifQuery = actifQuery.Where(p => 
                p.OperationCode == opCode || 
                (p.ModeleSourceId != null && p.ModeleSource!.OperationCode == opCode));
        }

        // PosteCode supprimé pour la fabrication

            var brouillon = await brouillonQuery
                .OrderByDescending(p => p.Version)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();

            var actif = await actifQuery
                .Select(p => (int?)p.Version)
                .FirstOrDefaultAsync();

            return Ok(new
            {
                success = true,
                hasBrouillon = brouillon != Guid.Empty,
                brouillonId = brouillon,
                hasActif = actif.HasValue,
                actifVersion = actif ?? 0
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPlan(Guid id)
    {
        var data = await _planService.GetPlanByIdAsync(id);
        return Ok(new { success = true, data });
    }

    [HttpPut("{id}/valeurs")]
    public async Task<IActionResult> MettreAJourValeurs(Guid id, [FromBody] UpdateValeursPlanRequestDto request)
    {
        var success = await _planService.MettreAJourValeursPlanAsync(
            id,
            request.Sections,
            request.LegendeMoyens,
            request.Remarques,
            request.Finaliser,
            request.LibelleSection,
            request.ModifiePar
        );

        if (!success) return NotFound(new { success = false, message = "Plan introuvable." });

        var msg = request.Finaliser ? "Plan mis à jour avec succès et rendu ACTIF." : "Plan mis à jour en tant que brouillon.";
        return Ok(new { success = true, message = msg });
    }

    [HttpPost("import-excel")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult> ImportExcel(IFormFile file, [FromForm] string? configurationColonnesJson, [FromServices] IExcelImportService excelService)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Aucun fichier sélectionné ou fichier vide." });

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) && !file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Seuls les fichiers Excel (.xlsx) ou CSV (.csv) sont supportés." });

        try
        {
            using var stream = file.OpenReadStream();
            var parsedData = await excelService.ParsePlanExcelAsync(stream, file.FileName, configurationColonnesJson);
            return Ok(new { message = "Import réussi", data = parsedData });
        }
        catch (Exception ex)
        {
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            return StatusCode(500, new { message = "Erreur lors de l'import : " + innerMessage });
        }
    }

    // ⚠️ NOUVELLE ROUTE : Suppression physique (Hard Delete)
    [HttpDelete("{id}")]
    public async Task<IActionResult> SupprimerBrouillon(Guid id)
    {
        try
        {
            var success = await _planService.SupprimerBrouillonAsync(id);
            if (!success) return NotFound(new { success = false, message = "Plan introuvable." });

            return Ok(new { success = true, message = "Le brouillon a été définitivement supprimé de la base de données." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("clone")]
    public async Task<IActionResult> ClonerPlan([FromBody] ClonePlanRequestDto request)
    {
        var id = await _planService.ClonerPlanPourNouvelArticleAsync(request);
        return Ok(new { success = true, planId = id, message = "Plan cloné avec succès." });
    }

    [HttpPost("nouvelle-version")]
    public async Task<IActionResult> CreerVersion([FromBody] NouvelleVersionRequestDto request)
    {
        var id = await _planService.CreerNouvelleVersionPlanAsync(request);
        return Ok(new { success = true, planId = id, message = "V2 générée avec succès." });
    }

    [HttpPost("restaurer")]
    public async Task<IActionResult> RestaurerPlan([FromBody] RestaurerPlanRequestDto request)
    {
        try
        {
            var id = await _planService.RestaurerPlanArchiveAsync(request);
            return Ok(new { success = true, planId = id, message = "Plan de fabrication restauré et activé avec succès." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("periodicites")]
    public async Task<IActionResult> CreerNouvellePeriodicite([FromBody] CreatePeriodiciteDto request)
    {
        try
        {
            var periodiciteId = await _referentielService.CreatePeriodiciteAsync(request);
            return Ok(new { success = true, periodiciteId });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("caracteristiques")]
    public async Task<IActionResult> CreerNouvelleCaracteristique([FromBody] CreateCaracteristiqueDto request)
    {
        try
        {
            var caracteristiqueId = await _referentielService.CreateCaracteristiqueAsync(request);
            var caracteristique = await _context.TypeCaracteristiques.FindAsync(caracteristiqueId);

            return Ok(new
            {
                success = true,
                caracteristiqueId,
                data = caracteristique
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

 
}
