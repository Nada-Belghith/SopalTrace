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

    public HubService(SopalTraceDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<HubModeleDto>> GetTousLesModelesAsync()
    {
        var result = new List<HubModeleDto>();

        // 1. FABRICATION (Modèles uniquement)
        var fabModeles = await _context.ModeleFabEntetes
            .AsNoTracking()
            .Select(m => new HubModeleDto(
                m.Id,
                "FAB",
                m.Libelle,
                m.NatureComposantCode,
                m.TypeRobinetCode,
                m.OperationCode ?? "N/A",
                m.Version,
                m.Statut,
                "Gabarit de fabrication générique."))
            .ToListAsync();
        result.AddRange(fabModeles);

        // 2. ASSEMBLAGE
        var assModeles = await _context.PlanAssEntetes
            .AsNoTracking()
            .Where(m => m.EstModele)
            .Select(m => new HubModeleDto(
                m.Id,
                "ASS",
                m.Nom,
                "N/A",
                m.TypeRobinetCode,
                m.OperationCode,
                m.Version,
                m.Statut,
                "Plan Maître d'assemblage."))
            .ToListAsync();
        result.AddRange(assModeles);

        // 3. VÉRIF MACHINE
        var vmModeles = await _context.PlanVerifMachineEntetes
            .AsNoTracking()
            .Select(m => new HubModeleDto(
                m.Id,
                "VM",
                m.Nom,
                "MACHINE",
                "VM",
                m.MachineCode,
                m.Version ?? 1,
                m.Statut,
                "Vérification des étalons machines."))
            .ToListAsync();
        result.AddRange(vmModeles);

        // 4. ÉCHANTILLONNAGE (Global Unique — pas de filtre par article)
        var echModeles = await _context.PlanEchantillonnageEntetes
            .AsNoTracking()
            .Join(_context.Nqas, m => m.NqaId, n => n.Id, (m, n) => new HubModeleDto(
                m.Id,
                "ECH",
                "Plan Global",
                "N/A",
                m.TypePlan,
                "NQA " + n.ValeurNqa,
                m.Version,
                m.Statut,
                "Niveau de contrôle: " + m.NiveauControle))
            .ToListAsync();
        result.AddRange(echModeles);

        // 5. PRODUIT FINI (Modèles uniquement : CodeArticleSage == null)
        var pfModeles = await _context.PlanPfEntetes
            .AsNoTracking()
            .Where(m => m.CodeArticleSage == null)
            .Select(m => new HubModeleDto(
                m.Id,
                "PF",
                string.IsNullOrWhiteSpace(m.Designation) ? $"Plan PF {m.TypeRobinetCode}" : m.Designation,
                "PRODUIT FINI",
                m.TypeRobinetCode,
                "CONTRÔLE FINAL",
                m.Version,
                m.Statut,
                "Gabarit de contrôle final."))
            .ToListAsync();
        result.AddRange(pfModeles);

        // 6. RÉSULTAT CONTRÔLE
        var ncModeles = await _context.PlanNcEntetes
            .AsNoTracking()
            .Select(m => new HubModeleDto(
                m.Id,
                "RC",
                m.Nom,
                "POSTE",
                "RC",
                m.PosteCode,
                m.Version ?? 1,
                m.Statut,
                "Fiche de contrôle par poste de travail."))
            .ToListAsync();
        result.AddRange(ncModeles);

        return result;
    }

    public async Task<IReadOnlyList<HubPlanDto>> GetTousLesPlansAsync()
    {
        var result = new List<HubPlanDto>();

        // 1. PLANS DE FABRICATION (Instanciés par article)
        var fabPlans = await _context.PlanFabEntetes
            .AsNoTracking()
            .Include(p => p.ModeleSource)
            .Select(p => new HubPlanDto(
                p.Id,
                "FAB",
                p.Nom ?? $"Plan {p.CodeArticleSage}",
                p.ModeleSource != null ? p.ModeleSource.NatureComposantCode : "N/A",
                p.ModeleSource != null ? (p.ModeleSource.TypeRobinetCode ?? "N/A") : "N/A",
                p.ModeleSource != null ? (p.ModeleSource.OperationCode ?? "N/A") : (p.OperationCode ?? "N/A"),
                p.Version,
                p.Statut,
                $"Plan article {p.CodeArticleSage}",
                p.CodeArticleSage,
                p.Designation))
            .ToListAsync();
        result.AddRange(fabPlans);

        // 2. PLANS PRODUIT FINI (Par article)
        var pfPlans = await _context.PlanPfEntetes
            .AsNoTracking()
            .Where(m => m.CodeArticleSage != null)
            .Select(m => new HubPlanDto(
                m.Id,
                "PF",
                string.IsNullOrWhiteSpace(m.Designation) ? $"Plan PF {m.CodeArticleSage}" : m.Designation,
                "PRODUIT FINI",
                m.TypeRobinetCode,
                "CONTRÔLE FINAL",
                m.Version,
                m.Statut,
                $"Plan final article {m.CodeArticleSage}",
                m.CodeArticleSage,
                m.Designation))
            .ToListAsync();
        result.AddRange(pfPlans);

        // 3. PLANS ÉCHANTILLONNAGE — désormais Global Unique, pas d'instances par article.
        // Les plans d'échantillonnage apparaissent uniquement dans la section Modèles/Configuration Globale.

        return result;
    }

    public async Task<bool> ChangerStatutModeleAsync(string category, Guid id, string statut)
    {
        switch (category)
        {
            case "FAB":
            {
                var m = await _context.ModeleFabEntetes.FindAsync(id);
                if (m is null) return false;
                m.Statut = statut;
                m.ArchiveLe = statut == "ARCHIVE" ? DateTime.UtcNow : null;
                m.ArchivePar = statut == "ARCHIVE" ? "ADMIN" : null;
                break;
            }
            case "ASS":
            {
                var m = await _context.PlanAssEntetes.FindAsync(id);
                if (m is null) return false;
                m.Statut = statut;
                m.ModifieLe = DateTime.UtcNow;
                m.ModifiePar = "ADMIN";
                break;
            }
            case "VM":
            {
                var m = await _context.PlanVerifMachineEntetes.FindAsync(id);
                if (m is null) return false;
                m.Statut = statut;
                m.ModifieLe = DateTime.UtcNow;
                m.ModifiePar = "ADMIN";
                break;
            }
            case "ECH":
            {
                var m = await _context.PlanEchantillonnageEntetes.FindAsync(id);
                if (m is null) return false;
                m.Statut = statut;
                break;
            }
            case "PF":
            {
                var m = await _context.PlanPfEntetes.FindAsync(id);
                if (m is null) return false;
                m.Statut = statut;
                m.ModifieLe = DateTime.UtcNow;
                m.ModifiePar = "ADMIN";
                break;
            }
            case "RC":
            {
                var m = await _context.PlanNcEntetes.FindAsync(id);
                if (m is null) return false;
                m.Statut = statut;
                m.ModifieLe = DateTime.UtcNow;
                m.ModifiePar = "ADMIN";
                break;
            }
            default:
                return false;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ChangerStatutPlanAsync(string category, Guid id, string statut)
    {
        switch (category)
        {
            case "FAB":
            {
                var p = await _context.PlanFabEntetes.FindAsync(id);
                if (p is null) return false;

                if (statut == "ARCHIVE" && p.Statut == "BROUILLON")
                {
                    return false;
                }

                p.Statut = statut;
                p.ModifieLe = DateTime.UtcNow;
                p.ModifiePar = "ADMIN";
                break;
            }
            default:
                return false;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SupprimerBrouillonPlanAsync(string category, Guid id)
    {
        switch (category)
        {
            case "FAB":
            {
                var plan = await _context.PlanFabEntetes
                    .Include(p => p.PlanFabSections)
                        .ThenInclude(s => s.PlanFabLignes)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (plan is null) return false;
                if (plan.Statut != "BROUILLON") return false;

                // Suppression complète : sections + lignes + entête
                foreach (var section in plan.PlanFabSections)
                {
                    _context.PlanFabLignes.RemoveRange(section.PlanFabLignes);
                }

                _context.PlanFabSections.RemoveRange(plan.PlanFabSections);
                _context.PlanFabEntetes.Remove(plan);

                await _context.SaveChangesAsync();
                return true;
            }
            default:
                return false;
        }
    }
}