using SopalTrace.Domain.Constants;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.DTOs.QualityPlans.Hub;
using SopalTrace.Application.Interfaces;
using SopalTrace.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Infrastructure.Services;

public class HubService : IHubService
{
    private readonly SopalTraceDbContext _context;

    private string GetCategoryFromTypeDocumentCode(string typeDocumentCode)
    {
        if (string.IsNullOrEmpty(typeDocumentCode)) return "INCONNU";
        if (typeDocumentCode.Contains("FAB")) return "FAB";
        if (typeDocumentCode.Contains("ASS")) return "ASS";
        if (typeDocumentCode.Contains("VM")) return "VM";
        if (typeDocumentCode.Contains("PF")) return "PF";
        if (typeDocumentCode == "RESULTAT_CF") return "RCCF";
        if (typeDocumentCode.Contains("RC") || typeDocumentCode.Contains("CTRL_POSTE")) return "RC";
        if (typeDocumentCode.Contains("ECH")) return "ECH";
        return typeDocumentCode;
    }

    public HubService(SopalTraceDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<HubModeleDto>> GetTousLesModelesAsync()
    {
        var result = new List<HubModeleDto>();

        var documents = await _context.DocumentEntetes
            .AsNoTracking()
            .Include(d => d.Formulaire)
            .Include(d => d.TypeDocumentCodeNavigation)
            .Where(d => d.Statut == StatutsPlan.Actif || d.Statut == StatutsPlan.Archive || d.Statut == StatutsPlan.Brouillon)
            .ToListAsync();

        foreach(var m in documents)
        {
            result.Add(new HubModeleDto(
                m.Id,
                GetCategoryFromTypeDocumentCode(m.TypeDocumentCode),
                m.Nom ?? m.Designation ?? "Document Sans Nom",
                m.NatureArticleCode ?? "N/A",
                m.FamilleProduitFiniCode ?? "GEN",
                m.OperationCode ?? "N/A",
                m.PosteCode ?? "N/A",
                m.Version,
                m.Statut ?? StatutsPlan.Actif,
                m.Remarques ?? "Document Générique.",
                m.Formulaire != null ? m.Formulaire.CodeReference : null,
                m.Formulaire != null ? m.Formulaire.Version : (int?)null
            ));
        }

        var vmModeles = await _context.PlanVerifMachineEntetes
            .AsNoTracking()
            .Include(v => v.Formulaire)
            .Where(v => v.Statut == StatutsPlan.Actif || v.Statut == StatutsPlan.Archive || v.Statut == StatutsPlan.Brouillon)
            .ToListAsync();

        foreach(var m in vmModeles)
        {
            result.Add(new HubModeleDto(
                m.Id,
                "VM",
                m.Nom ?? "Verif Machine",
                "N/A",
                "N/A",
                "N/A",
                m.MachineCode ?? "N/A",
                m.Version ?? 1,
                m.Statut ?? StatutsPlan.Actif,
                m.Remarques ?? "Modèle de vérification machine",
                m.Formulaire != null ? m.Formulaire.CodeReference : null,
                m.Formulaire != null ? m.Formulaire.Version : (int?)null
            ));
        }

        var echanModeles = await _context.PlanEchantillonnageEntetes
            .AsNoTracking()
            .Include(v => v.Formulaire)
            .Where(v => v.Statut == StatutsPlan.Actif || v.Statut == StatutsPlan.Archive || v.Statut == StatutsPlan.Brouillon)
            .ToListAsync();

        foreach(var m in echanModeles)
        {
            result.Add(new HubModeleDto(
                m.Id,
                "ECH",
                "Plan d'Échantillonnage",
                "N/A",
                "N/A",
                "N/A",
                "N/A",
                m.Version,
                m.Statut,
                m.Remarques ?? "Modèle d'échantillonnage",
                m.Formulaire != null ? m.Formulaire.CodeReference : null,
                m.Formulaire != null ? m.Formulaire.Version : (int?)null
            ));
        }

        return result;
    }

    public async Task<IReadOnlyList<HubPlanDto>> GetTousLesPlansAsync()
    {
        var result = new List<HubPlanDto>();

        var documents = await _context.DocumentEntetes
            .AsNoTracking()
            .Include(d => d.Formulaire)
            .Include(d => d.TypeDocumentCodeNavigation)
            .Where(d => d.Statut == StatutsPlan.Actif || d.Statut == StatutsPlan.Archive || d.Statut == StatutsPlan.Brouillon)
            .ToListAsync();

        foreach(var p in documents)
        {
            result.Add(new HubPlanDto(
                p.Id,
                GetCategoryFromTypeDocumentCode(p.TypeDocumentCode),
                p.Nom ?? p.Designation ?? "Plan",
                p.FamilleProduitFiniCode ?? "N/A",
                p.Designation ?? "N/A",
                p.OperationCode ?? "N/A",
                p.OperationCode ?? "N/A", // Wait, this was a duplicate OperationCode in the original? We'll keep it as is.
                p.Version,
                p.Statut ?? StatutsPlan.Actif,
                p.Remarques ?? "Plan de contrôle instancié",
                p.Nom,
                p.TypeDocumentCode ?? "INCONNU",
                p.Formulaire != null ? p.Formulaire.CodeReference : null,
                p.Formulaire != null ? p.Formulaire.Version : (int?)null
            ));
        }

        var vmPlans = await _context.PlanVerifMachineEntetes
            .AsNoTracking()
            .Include(v => v.Formulaire)
            .Where(v => v.Statut == StatutsPlan.Actif || v.Statut == StatutsPlan.Archive || v.Statut == StatutsPlan.Brouillon)
            .ToListAsync();

        foreach(var p in vmPlans)
        {
            result.Add(new HubPlanDto(
                p.Id,
                "VM",
                p.Nom ?? "Verif Machine",
                "N/A",
                "N/A",
                "N/A",
                p.MachineCode ?? "N/A",
                p.Version ?? 1,
                p.Statut ?? StatutsPlan.Actif,
                p.Remarques ?? "Plan de vérification machine",
                p.Nom,
                "VM",
                p.Formulaire != null ? p.Formulaire.CodeReference : null,
                p.Formulaire != null ? p.Formulaire.Version : (int?)null
            ));
        }

        var echanPlans = await _context.PlanEchantillonnageEntetes
            .AsNoTracking()
            .Include(v => v.Formulaire)
            .Where(v => v.Statut == StatutsPlan.Actif || v.Statut == StatutsPlan.Archive || v.Statut == StatutsPlan.Brouillon)
            .ToListAsync();

        foreach(var p in echanPlans)
        {
            result.Add(new HubPlanDto(
                p.Id,
                "ECH",
                "Plan d'Échantillonnage",
                "N/A",
                "N/A",
                "N/A",
                "N/A",
                p.Version,
                p.Statut,
                p.Remarques ?? "Plan d'échantillonnage",
                "Echantillonnage",
                "ECHANTILLONNAGE",
                p.Formulaire != null ? p.Formulaire.CodeReference : null,
                p.Formulaire != null ? p.Formulaire.Version : (int?)null
            ));
        }

        return result;
    }

    public async Task<IReadOnlyList<HubPlanDto>> GetToutesLesStructuresAsync()
    {
        var result = new List<HubPlanDto>();

        var prcStructures = await _context.RefFormulaires
            .AsNoTracking()
            .Where(f => f.Role == "EN_COURS_DE_FABRICATION")
            .Where(f => f.Statut == StatutsPlan.Actif || f.Statut == StatutsPlan.Archive)
            .Select(f => new HubPlanDto(
                f.Id,
                "PRC_STRUCT",
                "Structure PRC",
                "N/A",
                "N/A",
                "N/A",
                "N/A",
                f.Version,
                f.Statut,
                $"Structure de référence des plans en cours de fabrication",
                null,
                $"Structure de référence",
                f.CodeReference,
                f.Version
            ))
            .ToListAsync();
        result.AddRange(prcStructures);

        return result;
    }


    public async Task<bool> ChangerStatutModeleAsync(string category, Guid id, string statut)
    {
        var m = await _context.DocumentEntetes.FindAsync(id);
        if (m is null) return false;
        m.Statut = statut;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ChangerStatutPlanAsync(string category, Guid id, string statut)
    {
        var p = await _context.DocumentEntetes.FindAsync(id);
        if (p is null) return false;

        if (statut == StatutsPlan.Archive && p.Statut == StatutsPlan.Brouillon)
        {
            return false;
        }

        p.Statut = statut;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SupprimerBrouillonPlanAsync(string category, Guid id)
    {
        var plan = await _context.DocumentEntetes
            .Include(p => p.DocumentSections)
                .ThenInclude(s => s.DocumentLignes)
                    .ThenInclude(l => l.DocumentLigneExtraColonnes)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (plan is null) return false;
        if (plan.Statut != StatutsPlan.Brouillon) return false;

        foreach (var section in plan.DocumentSections)
        {
            foreach (var ligne in section.DocumentLignes)
            {
                _context.DocumentLigneExtraColonnes.RemoveRange(ligne.DocumentLigneExtraColonnes);
            }
            _context.DocumentLignes.RemoveRange(section.DocumentLignes);
        }

        _context.DocumentSections.RemoveRange(plan.DocumentSections);
        _context.DocumentEntetes.Remove(plan);

        await _context.SaveChangesAsync();
        return true;
    }
}
