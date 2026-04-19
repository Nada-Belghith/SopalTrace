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

        var vmModeles = await _context.PlanVerifMachineEntetes
            .AsNoTracking()
            .Select(m => new HubModeleDto(
                m.Id,
                "VM",
                m.Nom,
                "MACHINE",
                m.TypeRapport,
                m.MachineCode,
                m.Version,
                m.Statut,
                "Vérification des étalons machines."))
            .ToListAsync();
        result.AddRange(vmModeles);

        var echModeles = await _context.PlanEchantillonnageEntetes
            .AsNoTracking()
            .Where(m => m.CodeArticleSage == null)
            .Join(_context.Nqas, m => m.NqaId, n => n.Id, (m, n) => new HubModeleDto(
                m.Id,
                "ECH",
                "Profil: " + m.CodeReference,
                "N/A",
                m.TypePlan,
                "NQA " + n.ValeurNqa,
                m.Version,
                m.Statut,
                "Niveau de contrôle: " + m.NiveauControle))
            .ToListAsync();
        result.AddRange(echModeles);

        return result;
    }

    public async Task<IReadOnlyList<HubPlanDto>> GetTousLesPlansAsync()
    {
        var result = new List<HubPlanDto>();

        var fabPlans = await _context.PlanFabEntetes
            .AsNoTracking()
            .Include(p => p.ModeleSource)
            .Select(p => new HubPlanDto(
                p.Id,
                "FAB",
                p.Nom ?? "N/A",
                p.ModeleSource.NatureComposantCode,
                p.ModeleSource.TypeRobinetCode ?? "N/A",
                p.ModeleSource.OperationCode ?? "N/A",
                p.Version,
                p.Statut,
                $"Plan article {p.CodeArticleSage}"))
            .ToListAsync();
        result.AddRange(fabPlans);

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
                var plan = await _context.PlanFabEntetes.FirstOrDefaultAsync(p => p.Id == id);

                if (plan is null) return false;
                if (plan.Statut != "BROUILLON") return false;

                // Suppression logique: la suppression physique est bloquée par le trigger SQL trg_no_del_PlanFab.
                plan.Statut = "OBSOLETE";
                plan.ModifieLe = DateTime.UtcNow;
                plan.ModifiePar = "ADMIN";

                await _context.SaveChangesAsync();
                return true;
            }
            default:
                return false;
        }
    }
}