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
            .Where(m => m.Statut == "ACTIF" || m.Statut == "ARCHIVE" || m.Statut == "BROUILLON")
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
                "Gabarit de fabrication générique.",
                m.Formulaire != null ? m.Formulaire.CodeReference : null))
            .ToListAsync();
        result.AddRange(fabModeles);

        // 2. ASSEMBLAGE
        var assModeles = await _context.PlanAssemblageEntetes
            .AsNoTracking()
            .Include(m => m.Formulaire)
            .Where(m => m.Statut == "ACTIF" || m.Statut == "ARCHIVE" || m.Statut == "BROUILLON")
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
                m.Formulaire != null ? m.Formulaire.Version : m.Version,  // Version du RefFormulaire (source de vérité)
                m.Statut ?? "ACTIF",
                "Plan Maître d'assemblage.",
                m.Formulaire != null ? m.Formulaire.CodeReference : null))
            .ToListAsync();
        result.AddRange(assModeles);

        // 3. VÉRIF MACHINE
        var vmModeles = await _context.PlanVerifMachineEntetes
            .AsNoTracking()
            .Include(m => m.Formulaire)
            .Where(m => m.Statut == "ACTIF" || m.Statut == "ARCHIVE" || m.Statut == "BROUILLON")
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
                "Vérification des étalons machines.",
                m.Formulaire != null ? m.Formulaire.CodeReference : null))
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
                "Niveau de contrôle: " + m.NiveauControle,
                null))
            .ToListAsync();
        result.AddRange(echModeles);

        // 5. PRODUIT FINI
        var pfModeles = await _context.PlanProduitFiniEntetes
            .AsNoTracking()
            .Include(m => m.Formulaire)
            .Where(m => m.Statut == "ACTIF" || m.Statut == "ARCHIVE" || m.Statut == "BROUILLON")
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
                "Gabarit de controle final.",
                m.Formulaire != null ? m.Formulaire.CodeReference : null))
            .ToListAsync();
        result.AddRange(pfModeles);

        // 6. RÉSULTAT CONTRÔLE POSTE
        var ncModeles = await _context.PlanControlePosteEntetes
            .AsNoTracking()
            .Include(m => m.Formulaire)
            .Where(m => m.Statut == "ACTIF" || m.Statut == "ARCHIVE" || m.Statut == "BROUILLON")
            .Select(m => new HubModeleDto(
                m.Id,
                "RC",
                "Résultat de contrôle poste " + (m.PosteCode ?? "N/A"),
                "POSTE",
                null,
                null,
                m.PosteCode ?? "N/A",
                m.Formulaire != null ? m.Formulaire.Version : m.Version,
                m.Formulaire != null ? m.Formulaire.Statut : m.Statut,
                "Fiche de contrôle par poste de travail.",
                m.Formulaire != null ? m.Formulaire.CodeReference : null))
            .ToListAsync();
        result.AddRange(ncModeles);

        // 7. RÉSULTAT CONTRÔLE CF
        var rccfModeles = await _context.PlanResultatControleCfEntetes
            .AsNoTracking()
            .Include(m => m.Formulaire)
            .Where(m => m.Statut == "ACTIF" || m.Statut == "ARCHIVE" || m.Statut == "BROUILLON")
            .Select(m => new HubModeleDto(
                m.Id,
                "RCCF",
                m.Nom ?? "Résultat Contrôle CF Sans Nom",
                "POSTE",
                null,
                null,
                m.PosteCode ?? "N/A",
                m.Formulaire != null ? m.Formulaire.Version : m.Version,
                m.Formulaire != null ? m.Formulaire.Statut : m.Statut,
                "Résultat Contrôle en cours de fabrication.",
                m.Formulaire != null ? m.Formulaire.CodeReference : null))
            .ToListAsync();
        result.AddRange(rccfModeles);

        return result;
    }

    public async Task<IReadOnlyList<HubPlanDto>> GetTousLesPlansAsync()
    {
        var result = new List<HubPlanDto>();

        // 1. STRUCTURES GENERIQUES (REF FORMULAIRES PRC)
        var prcStructures = await _context.RefFormulaires
            .AsNoTracking()
            .Where(f => f.Role == "EN_COURS_DE_FABRICATION")
            .Where(f => f.Statut == "ACTIF" || f.Statut == "ARCHIVE")
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
                f.CodeReference
            ))
            .ToListAsync();
        result.AddRange(prcStructures);

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
                var m = await _context.PlanControlePosteEntetes.FindAsync(id);
                if (m is null) return false;
                m.Statut = statut;
                //m.ModifieLe = DateTime.UtcNow;
                //m.ModifiePar = "ADMIN";
                break;
            }
            case "RCCF":
            {
                var m = await _context.PlanResultatControleCfEntetes.FindAsync(id);
                if (m is null) return false;
                m.Statut = statut;
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
                var plan = await _context.PlanControlePosteEntetes
                    .Include(p => p.PlanControlePosteLignes)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (plan is null) return false;
                if (plan.Statut != "BROUILLON") return false;

                _context.PlanControlePosteLignes.RemoveRange(plan.PlanControlePosteLignes);
                _context.PlanControlePosteEntetes.Remove(plan);

                await _context.SaveChangesAsync();
                return true;
            }
            case "RCCF":
            {
                var plan = await _context.PlanResultatControleCfEntetes
                    .Include(p => p.PlanResultatControleCfSections)
                        .ThenInclude(s => s.PlanResultatControleCfLignes)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (plan is null) return false;
                if (plan.Statut != "BROUILLON") return false;

                foreach (var section in plan.PlanResultatControleCfSections)
                {
                    _context.PlanResultatControleCfLignes.RemoveRange(section.PlanResultatControleCfLignes);
                }
                _context.PlanResultatControleCfSections.RemoveRange(plan.PlanResultatControleCfSections);
                _context.PlanResultatControleCfEntetes.Remove(plan);

                await _context.SaveChangesAsync();
                return true;
            }
            default:
                return false;
        }
    }
}
