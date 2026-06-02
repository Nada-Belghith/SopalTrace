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
        var fabModeles = await _context.ModeleFabricationEntetes
            .AsNoTracking()
            .Include(m => m.Formulaire)
            .Where(m => m.Statut == "ACTIF")
            .Select(m => new HubModeleDto(
                m.Id,
                "FAB",
                (m.NatureArticleCode == "PF") ? "Plan en cours de fabrication produit fini" :
                (m.NatureArticleCode == "PISTON") ? "Plan de contrôle en cours de fabrication piston" :
                (m.Libelle ?? "Modèle Sans Nom"),
                m.NatureArticleCode ?? "N/A",
                "GEN",
                m.OperationCode ?? "N/A",
                "N/A",
                m.Formulaire != null ? m.Formulaire.Version : m.Version,
                m.Statut ?? "ACTIF",
                "Gabarit de fabrication générique."))
            .ToListAsync();
        result.AddRange(fabModeles);

        // 2. ASSEMBLAGE
        var assModeles = await _context.PlanAssemblageEntetes
            .AsNoTracking()
            .Include(m => m.Formulaire)
            .Where(m => m.Statut == "ACTIF")
            .Select(m => new HubModeleDto(
                m.Id,
                "ASS",
                (m.NatureArticleCode == "PF") ? "Plan en cours de fabrication produit fini" :
                (m.NatureArticleCode == "PISTON") ? "Plan de contrôle en cours de fabrication piston" :
                (m.Designation ?? "Plan Sans Nom"),
                m.NatureArticleCode ?? "PF",
                m.FamilleProduitFiniCode ?? "N/A",
                m.OperationCode ?? "N/A",
                m.PosteCode ?? "N/A",
                m.Formulaire != null ? m.Formulaire.Version : m.Version,
                m.Statut ?? "ACTIF",
                "Plan Maître d'assemblage."))
            .ToListAsync();
        result.AddRange(assModeles);

        // 3. VÉRIF MACHINE
        var vmModeles = await _context.PlanVerifMachineEntetes
            .AsNoTracking()
            .Include(m => m.Formulaire)
            .Where(m => m.Statut == "ACTIF")
            .Select(m => new HubModeleDto(
                m.Id,
                "VM",
                m.Nom ?? "Machine Sans Nom",
                "MACHINE",
                "VM",
                "VÉRIF",
                m.MachineCode ?? "N/A",
                m.Formulaire != null ? m.Formulaire.Version : (m.Version ?? 1),
                m.Statut ?? "ACTIF",
                "Vérification des étalons machines."))
            .ToListAsync();
        result.AddRange(vmModeles);

        // 4. ÉCHANTILLONNAGE
        var echModeles = await _context.PlanEchantillonnageEntetes
            .AsNoTracking()
            .Join(_context.Nqas, m => m.NqaId, n => n.Id, (m, n) => new HubModeleDto(
                m.Id,
                "ECH",
                "Plan Global",
                "N/A",
                m.TypePlan ?? "N/A",
                "NQA",
                n.ValeurNqa.ToString(),
                m.Version,
                m.Statut ?? "ACTIF",
                "Niveau de contrôle: " + m.NiveauControle))
            .ToListAsync();
        result.AddRange(echModeles);

        // 5. PRODUIT FINI
        var pfModeles = await _context.PlanProduitFiniEntetes
            .AsNoTracking()
            .Where(m => m.Statut == "ACTIF")
            .Select(m => new HubModeleDto(
                m.Id,
                "PF",
                "Plan de contrôle produit fini",
                "PRODUIT FINI",
                m.FamilleProduitFiniCode ?? "PF",
                "CONTROLE FINAL",
                "N/A",
                m.Version,
                m.Statut ?? "ACTIF",
                "Gabarit de controle final."))
            .ToListAsync();
        result.AddRange(pfModeles);

        // 6. RÉSULTAT CONTRÔLE
        var ncModeles = await _context.PlanNonConformiteEntetes
            .AsNoTracking()
            .Where(m => m.Statut == "ACTIF")
            .Select(m => new HubModeleDto(
                m.Id,
                "RC",
                m.Nom ?? "Fiche Sans Nom",
                "POSTE",
                "RC",
                "CONTRÔLE",
                m.PosteCode ?? "N/A",
                m.Version,
                m.Statut,
                "Fiche de contrôle par poste de travail."))
            .ToListAsync();
        result.AddRange(ncModeles);

        return result;
    }

    public async Task<IReadOnlyList<HubPlanDto>> GetTousLesPlansAsync()
    {
        var result = new List<HubPlanDto>();
 
        // 1. PLANS DE FABRICATION
        var fabPlans = await _context.PlanFabricationEntetes
            .AsNoTracking()
            .Include(p => p.Formulaire)
            .Where(p => p.Statut == "ACTIF")
            .Include(p => p.ModeleSource)
            .Select(p => new HubPlanDto(
                p.Id,
                "FAB",
                // ✅ Priorité au Nom stocké, SAUF si c'est un nom technique hérité
                !string.IsNullOrWhiteSpace(p.Nom) && !p.Nom.StartsWith("Modèle ") && !p.Nom.StartsWith("PC-") ? p.Nom :
                (p.ModeleSource != null && p.ModeleSource.NatureArticleCode == "PF") ? "Plan en cours de fabrication produit fini" :
                (p.ModeleSource != null && p.ModeleSource.NatureArticleCode == "PISTON") ? "Plan de contrôle en cours de fabrication piston" :
                $"Plan {p.CodeArticleSage}",
                p.ModeleSource != null ? p.ModeleSource.NatureArticleCode : "N/A",
                p.CodeArticleSageNavigation.ProduitFini != null ? p.CodeArticleSageNavigation.ProduitFini.FamilleProduitFiniCode : "FAB",
                p.ModeleSource != null ? (p.ModeleSource.OperationCode ?? "N/A") : (p.OperationCode ?? "N/A"),
                "N/A",
                p.Formulaire != null ? p.Formulaire.Version : p.Version,
                p.Statut,
                $"Plan article {p.CodeArticleSage}",
                p.CodeArticleSage,
                p.Designation))
            .ToListAsync();
        result.AddRange(fabPlans);
  
        // 2. PLANS D'ASSEMBLAGE (PISTON / PF : plans article, non modèles)
        // Plans ASS : PISTON, PF, ou ceux avec nature NULL (créés via un bug de routing)
        var assPlans = await _context.PlanAssemblageEntetes
            .AsNoTracking()
            .Include(p => p.Formulaire)
            .Where(p => p.NatureArticleCode == "PISTON" || p.NatureArticleCode == "PF" || p.NatureArticleCode == null)
            .Where(p => p.OperationCode == "ASS") // Exclure les modèles génériques
            .Where(p => p.Statut == "ACTIF") // Seulement les versions actives
            .Select(p => new HubPlanDto(
                p.Id,
                "FAB",
                // ✅ Nom : ignorer les noms techniques ("Modèle ...", "PC-..."), inférer depuis la nature
                (!string.IsNullOrWhiteSpace(p.Designation) && !p.Designation.StartsWith("Modèle ") && !p.Designation.StartsWith("PC-")) ? p.Designation :
                (p.NatureArticleCode == "PISTON") ? "Plan de contrôle en cours de fabrication piston" :
                (p.NatureArticleCode == "PF") ? "Plan en cours de fabrication produit fini" :
                // Nature NULL : essayer de deviner depuis la Designation
                (!string.IsNullOrWhiteSpace(p.Designation) && p.Designation.ToLower().Contains("piston")) ? "Plan de contrôle en cours de fabrication piston" :
                "Plan Assemblage",
                p.NatureArticleCode ?? "N/A",
                p.FamilleProduitFiniCode ?? "ASS",
                p.OperationCode ?? "N/A",
                p.PosteCode ?? "N/A",
                p.Formulaire != null ? p.Formulaire.Version : p.Version,
                p.Statut ?? "BROUILLON",
                $"Plan assemblage {p.NatureArticleCode ?? "ASS"}",
                null,
                p.Designation))
            .ToListAsync();
        result.AddRange(assPlans);

        return result;
    }


    public async Task<bool> ChangerStatutModeleAsync(string category, Guid id, string statut)
    {
        switch (category)
        {
            case "FAB":
            {
                var m = await _context.ModeleFabricationEntetes.FindAsync(id);
                if (m is null) return false;
                m.Statut = statut;
                //m.ArchiveLe = statut == "ARCHIVE" ? DateTime.UtcNow : null;
                //m.ArchivePar = statut == "ARCHIVE" ? "ADMIN" : null;
                break;
            }
            case "ASS":
            {
                var m = await _context.PlanAssemblageEntetes.FindAsync(id);
                if (m is null) return false;
                m.Statut = statut;
                //m.ModifieLe = DateTime.UtcNow;
                //m.ModifiePar = "ADMIN";
                break;
            }
            case "VM":
            {
                var m = await _context.PlanVerifMachineEntetes.FindAsync(id);
                if (m is null) return false;
                m.Statut = statut;
                //m.ModifieLe = DateTime.UtcNow;
                //m.ModifiePar = "ADMIN";
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
                var m = await _context.PlanProduitFiniEntetes.FindAsync(id);
                if (m is null) return false;
                m.Statut = statut;
                //m.ModifieLe = DateTime.UtcNow;
                //m.ModifiePar = "ADMIN";
                break;
            }
            case "RC":
            {
                var m = await _context.PlanNonConformiteEntetes.FindAsync(id);
                if (m is null) return false;
                m.Statut = statut;
                //m.ModifieLe = DateTime.UtcNow;
                //m.ModifiePar = "ADMIN";
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
                var p = await _context.PlanFabricationEntetes.FindAsync(id);
                if (p is null)
                {
                    var assPlan = await _context.PlanAssemblageEntetes.FindAsync(id);
                    if (assPlan is null) return false;

                    if (statut == "ARCHIVE" && assPlan.Statut == "BROUILLON")
                    {
                        return false;
                    }

                    assPlan.Statut = statut;
                    break;
                }

                if (statut == "ARCHIVE" && p.Statut == "BROUILLON")
                {
                    return false;
                }

                p.Statut = statut;
                //p.ModifieLe = DateTime.UtcNow;
                //p.ModifiePar = "ADMIN";
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
                var plan = await _context.PlanFabricationEntetes
                    .Include(p => p.PlanFabricationSections)
                        .ThenInclude(s => s.PlanFabricationLignes)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (plan is null)
                {
                    var assPlan = await _context.PlanAssemblageEntetes
                        .Include(p => p.PlanAssemblageSections)
                            .ThenInclude(s => s.PlanAssemblageLignes)
                        .FirstOrDefaultAsync(p => p.Id == id);

                    if (assPlan is null) return false;
                    if (assPlan.Statut != "BROUILLON") return false;

                    foreach (var section in assPlan.PlanAssemblageSections)
                    {
                        _context.PlanAssemblageLignes.RemoveRange(section.PlanAssemblageLignes);
                    }

                    _context.PlanAssemblageSections.RemoveRange(assPlan.PlanAssemblageSections);
                    _context.PlanAssemblageEntetes.Remove(assPlan);

                    await _context.SaveChangesAsync();
                    return true;
                }

                if (plan.Statut != "BROUILLON") return false;

                // Suppression complète : sections + lignes + entête
                foreach (var section in plan.PlanFabricationSections)
                {
                    _context.PlanFabricationLignes.RemoveRange(section.PlanFabricationLignes);
                }

                _context.PlanFabricationSections.RemoveRange(plan.PlanFabricationSections);
                _context.PlanFabricationEntetes.Remove(plan);

                await _context.SaveChangesAsync();
                return true;
            }
            case "RC":
            {
                var plan = await _context.PlanNonConformiteEntetes
                    .Include(p => p.PlanNonConformiteLignes)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (plan is null) return false;
                if (plan.Statut != "BROUILLON") return false;

                _context.PlanNonConformiteLignes.RemoveRange(plan.PlanNonConformiteLignes);
                _context.PlanNonConformiteEntetes.Remove(plan);

                await _context.SaveChangesAsync();
                return true;
            }
            default:
                return false;
        }
    }
}
