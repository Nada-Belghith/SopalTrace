using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Api.Controllers;

[Route("api/magasinier")]
[ApiController]
public class MagasinierController : ControllerBase
{
    private readonly SopalTraceDbContext _context;

    public MagasinierController(SopalTraceDbContext context)
    {
        _context = context;
    }

    [HttpGet("of/{numeroOf}")]
    public async Task<IActionResult> GetOfDetails(string numeroOf)
    {
        if (string.IsNullOrWhiteSpace(numeroOf))
            return BadRequest(new { message = "Le numéro d'OF est requis." });

        var ofEntry = await _context.MfgheadOrdreFabrications
            .Include(o => o.CodeArticleNavigation)
                .ThenInclude(a => a.NatureArticleCodeNavigation)
            .Include(o => o.MfgmatBesoinOfs)
                .ThenInclude(m => m.CodeArticleNavigation)
                    .ThenInclude(a => a.NatureArticleCodeNavigation)
            .FirstOrDefaultAsync(o => o.NumeroOf.ToLower() == numeroOf.Trim().ToLower());

        if (ofEntry == null)
            return NotFound(new { message = $"Ordre de fabrication '{numeroOf}' introuvable dans le système." });

        var nomenclature = ofEntry.MfgmatBesoinOfs.Select(m => new
        {
            code = m.CodeArticle,
            designation = m.CodeArticleNavigation?.Designation ?? "Composant sans désignation",
            quantite = m.QuantiteRequise,
            nature = m.CodeArticleNavigation?.NatureArticleCodeNavigation?.Libelle ?? "COMPOSANT"
        }).ToList();

        var result = new
        {
            numeroOF = ofEntry.NumeroOf,
            articleCode = ofEntry.CodeArticle,
            articleDesignation = ofEntry.CodeArticleNavigation?.Designation ?? "Article sans désignation",
            articleNatureCode = ofEntry.CodeArticleNavigation?.NatureArticleCode ?? "SF",
            articleNatureLibelle = ofEntry.CodeArticleNavigation?.NatureArticleCodeNavigation?.Libelle ?? "Semi-Fini",
            quantitePrevue = ofEntry.QuantitePrevue,
            nomenclature = nomenclature
        };

        return Ok(new { success = true, data = result });
    }

    [HttpPost("preparation")]
    public async Task<IActionResult> SavePreparation([FromBody] SavePreparationRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.NumeroOF))
            return BadRequest(new { message = "Données de préparation invalides." });

        var ofEntry = await _context.MfgheadOrdreFabrications
            .FirstOrDefaultAsync(o => o.NumeroOf == request.NumeroOF);

        if (ofEntry == null)
            return NotFound(new { message = $"L'OF '{request.NumeroOF}' n'existe pas." });

        // Trouver un magasinier ou prendre le premier admin pour le mock si non fourni
        var matriculeMagasinier = request.MatriculeMagasinier ?? "MAG01";
        var magasinierExists = await _context.UtilisateursApps.AnyAsync(u => u.Matricule == matriculeMagasinier);
        if (!magasinierExists)
        {
            var firstUser = await _context.UtilisateursApps.FirstOrDefaultAsync();
            matriculeMagasinier = firstUser?.Matricule ?? "ADMIN";
        }

        var prep = new MagPreparationOf
        {
            Id = Guid.NewGuid(),
            NumeroOf = request.NumeroOF,
            MatriculeMagasinier = matriculeMagasinier,
            Statut = "TERMINE", // Respecte la contrainte de validation CHECK (Statut IN ('EN_COURS', 'TERMINE'))
            DateDebut = DateTime.UtcNow.AddMinutes(-30),
            DateFin = DateTime.UtcNow
        };

        _context.MagPreparationOfs.Add(prep);

        foreach (var item in request.Items)
        {
            if (string.IsNullOrWhiteSpace(item.Lots)) continue;

            var lotList = item.Lots.Split(';').Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l));
            foreach (var lot in lotList)
            {
                var prepLot = new MagPreparationOfLot
                {
                    Id = Guid.NewGuid(),
                    PreparationOfid = prep.Id,
                    CodeArticle = item.Code,
                    NumeroLotScanne = lot,
                    Quantite = item.Quantite,
                    DateScan = DateTime.UtcNow
                };
                _context.MagPreparationOfLots.Add(prepLot);
            }

            // Enregistrer le rapport de contrôle rapide (QC) s'il est renseigné dans le QR code
            if (!string.IsNullOrWhiteSpace(item.RapportQC))
            {
                var qcRapport = new MagQuickControlRapport
                {
                    Id = Guid.NewGuid(),
                    NumeroOf = request.NumeroOF,
                    CodeArticle = item.Code,
                    NumeroRapportQc = item.RapportQC.Trim(),
                    DateScan = DateTime.UtcNow
                };
                _context.MagQuickControlRapports.Add(qcRapport);
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "La préparation de l'OF a été validée avec succès !" });
    }
}

public class SavePreparationRequest
{
    public string NumeroOF { get; set; } = null!;
    public string? MatriculeMagasinier { get; set; }
    public System.Collections.Generic.List<PreparationItemRequest> Items { get; set; } = new();
}

public class PreparationItemRequest
{
    public string Code { get; set; } = null!;
    public string Lots { get; set; } = null!;
    public double Quantite { get; set; }
    public string? RapportQC { get; set; }
}
