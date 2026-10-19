using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;
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

    private string GetCategoryFromTypeDocumentCode(string? typeDocumentCode)
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

    private sealed record LigneStatistique(string Portee, string TypeDocumentCode, string Libelle, string Statut, int Nombre);

    /// <summary>Une seule requête groupée : portée × type de document × statut.</summary>
    public async Task<HubStatistiquesDto> GetStatistiquesAsync()
    {
        var lignes = await _context.DocumentEntetes
            .AsNoTracking()
            .GroupBy(d => new { d.Portee, d.TypeDocumentCode, Libelle = d.TypeDocumentCodeNavigation.Libelle, d.Statut })
            .Select(g => new LigneStatistique(g.Key.Portee, g.Key.TypeDocumentCode, g.Key.Libelle, g.Key.Statut, g.Count()))
            .ToListAsync();

        static CompteStatutsDto Compter(IEnumerable<LigneStatistique> groupe)
        {
            var liste = groupe.ToList();
            int Somme(string statut) => liste.Where(l => l.Statut == statut).Sum(l => l.Nombre);
            return new CompteStatutsDto(Somme(StatutsPlan.Brouillon), Somme(StatutsPlan.Actif), Somme(StatutsPlan.Archive));
        }

        var parType = lignes
            .GroupBy(l => new { l.TypeDocumentCode, l.Libelle, l.Portee })
            .Select(g => new StatistiquesTypeDocumentDto(g.Key.TypeDocumentCode, g.Key.Libelle, g.Key.Portee, Compter(g)))
            .OrderBy(t => t.Portee).ThenBy(t => t.Libelle)
            .ToList();

        return new HubStatistiquesDto(
            Compter(lignes.Where(l => l.Portee == PorteesDocument.Generique)),
            Compter(lignes.Where(l => l.Portee == PorteesDocument.Specifique)),
            parType);
    }

    public async Task<IReadOnlyList<HubModeleDto>> GetTousLesModelesAsync()
    {
        var result = new List<HubModeleDto>();

        var documents = await _context.DocumentEntetes
            .AsNoTracking()
            .Include(d => d.Formulaire)
            .Include(d => d.TypeDocumentCodeNavigation)
            .Where(d => d.TypeDocumentCode != TypesDocument.ModeleFabrication && d.TypeDocumentCode != TypesDocument.PlanFabrication
                     && d.TypeDocumentCode != TypesDocument.VerifMachine && d.TypeDocumentCode != TypesDocument.PlanEchantillonnage)
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
                m.Formulaire != null ? m.Formulaire.Version : (int?)null,
                m.CreeLe
            ));
        }

        var fabModeles = await _context.DocumentEntetes
            .AsNoTracking()
            .Include(d => d.Formulaire)
            .Where(d => d.TypeDocumentCode == TypesDocument.ModeleFabrication)
            .Where(d => d.Statut == StatutsPlan.Actif || d.Statut == StatutsPlan.Archive || d.Statut == StatutsPlan.Brouillon)
            .ToListAsync();

        foreach(var m in fabModeles)
        {
            result.Add(new HubModeleDto(
                m.Id,
                "FAB",
                m.Designation ?? m.Nom ?? "Modèle de Fabrication",
                m.NatureArticleCode ?? "N/A",
                m.FamilleProduitFiniCode ?? "GEN",
                m.OperationCode ?? "N/A",
                "N/A",
                m.Version,
                m.Statut ?? StatutsPlan.Actif,
                m.Remarques ?? "Modèle de Fabrication.",
                m.Formulaire != null ? m.Formulaire.CodeReference : null,
                m.Formulaire != null ? m.Formulaire.Version : (int?)null,
                m.CreeLe
            ));
        }

        var vmModeles = await _context.DocumentEntetes
            .AsNoTracking()
            .Include(v => v.Formulaire)
            .Where(v => v.TypeDocumentCode == TypesDocument.VerifMachine)
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
                m.Version,
                m.Statut,
                m.Remarques ?? "Modèle de vérification machine",
                m.Formulaire != null ? m.Formulaire.CodeReference : null,
                m.Formulaire != null ? m.Formulaire.Version : (int?)null,
                m.CreeLe
            ));
        }

        var echanModeles = await _context.DocumentEntetes
            .AsNoTracking()
            .Include(v => v.Formulaire)
            .Where(v => v.TypeDocumentCode == TypesDocument.PlanEchantillonnage)
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
                m.Formulaire != null ? m.Formulaire.Version : (int?)null,
                m.CreeLe
            ));
        }

        return result;
    }

    public async Task<IReadOnlyList<HubPlanDto>> GetTousLesPlansAsync()
    {
        var result = new List<HubPlanDto>();

        var fabPlans = await _context.DocumentEntetes
            .AsNoTracking()
            .Include(d => d.Formulaire)
            .Where(d => d.TypeDocumentCode == TypesDocument.PlanFabrication)
            .Where(d => d.Statut == StatutsPlan.Actif || d.Statut == StatutsPlan.Archive || d.Statut == StatutsPlan.Brouillon)
            .ToListAsync();

        foreach(var p in fabPlans)
        {
            result.Add(new HubPlanDto(
                p.Id,
                "FAB",
                p.Nom ?? p.Designation ?? "Plan par Article",
                p.CodeArticleSageVersionne ?? "N/A",
                p.Designation ?? "N/A",
                p.OperationCode ?? "N/A",
                p.OperationCode ?? "N/A", 
                p.Version,
                p.Statut ?? StatutsPlan.Actif,
                p.Remarques ?? "Plan de fabrication instancié",
                p.Nom,
                "PLAN_FAB",
                p.Formulaire != null ? p.Formulaire.CodeReference : null,
                p.Formulaire != null ? p.Formulaire.Version : (int?)null,
                p.CreeLe
            ));
        }

        return result;
    }

    public async Task<IReadOnlyList<HubPlanDto>> GetToutesLesStructuresAsync()
    {
        var result = new List<HubPlanDto>();

        var prcStructures = await _context.RefFormulaires
            .AsNoTracking()
            .Where(f => f.Role == RolesFormulaire.EnCoursDeFabrication)
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
                f.Version,
                f.CreeLe
            ))
            .ToListAsync();
        result.AddRange(prcStructures);

        return result;
    }


    public async Task<bool> ChangerStatutModeleAsync(string category, Guid id, string statut)
    {
        category = category?.ToUpperInvariant() ?? "";
        if (category == "FAB")
        {
            var m = await _context.DocumentEntetes.FirstOrDefaultAsync(d => d.Id == id && d.TypeDocumentCode == TypesDocument.ModeleFabrication);
            if (m is null) return false;
            m.Statut = statut;
        }
        else if (category == "VM")
        {
            var m = await _context.DocumentEntetes.FirstOrDefaultAsync(d => d.Id == id && d.TypeDocumentCode == TypesDocument.VerifMachine);
            if (m is null) return false;
            m.Statut = statut;
        }
        else if (category == "ECH")
        {
            var m = await _context.DocumentEntetes.FirstOrDefaultAsync(d => d.Id == id && d.TypeDocumentCode == TypesDocument.PlanEchantillonnage);
            if (m is null) return false;
            m.Statut = statut;
        }
        else
        {
            var m = await _context.DocumentEntetes.FindAsync(id);
            if (m is null) return false;
            m.Statut = statut;
        }

        await VerifierReactivationsAsync();
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Un document générique archivé dont le formulaire (code + version) est archivé reste archivé : le réactiver ferait
    /// revivre une identité remplacée (nouvelle version, changement de code). La base le refuse aussi.
    /// </summary>
    private async Task VerifierReactivationsAsync()
    {
        var reactives = _context.ChangeTracker.Entries<DocumentEntete>()
            .Where(e => e.State == EntityState.Modified && e.Entity.Statut == StatutsPlan.Actif
                        && e.Entity.Portee == PorteesDocument.Generique && e.Entity.FormulaireId != null
                        && e.Property(d => d.Statut).OriginalValue != StatutsPlan.Actif)
            .Select(e => e.Entity)
            .ToList();
        foreach (var document in reactives)
        {
            var formulaire = await _context.RefFormulaires.AsNoTracking()
                .Where(f => f.Id == document.FormulaireId && f.Statut == StatutsPlan.Archive)
                .Select(f => new { f.CodeReference, f.Version })
                .FirstOrDefaultAsync();
            if (formulaire != null)
                throw new RegleMetierException(
                    $"« {document.Nom} » appartient au formulaire {formulaire.CodeReference}.{formulaire.Version}, archivé : il reste archivé (traçabilité). Créez une nouvelle version du document.");
        }
    }

    public async Task<bool> ChangerStatutPlanAsync(string category, Guid id, string statut)
    {
        category = category?.ToUpperInvariant() ?? "";
        if (category == "FAB")
        {
            var p = await _context.DocumentEntetes.FirstOrDefaultAsync(d => d.Id == id && d.TypeDocumentCode == TypesDocument.PlanFabrication);
            if (p is null) return false;

            if (statut == StatutsPlan.Archive && p.Statut == StatutsPlan.Brouillon)
            {
                return false;
            }

            if (statut == StatutsPlan.Actif)
            {
                var otherActivePlans = await _context.DocumentEntetes
                    .Where(x => x.TypeDocumentCode == TypesDocument.PlanFabrication
                             && x.CodeArticleSageVersionne == p.CodeArticleSageVersionne 
                             && x.OperationCode == p.OperationCode 
                             && x.Statut == StatutsPlan.Actif 
                             && x.Id != p.Id)
                    .ToListAsync();
                foreach (var activePlan in otherActivePlans)
                {
                    activePlan.Statut = StatutsPlan.Archive;
                }
            }

            p.Statut = statut;
        }
        else if (category == "VM")
        {
            var p = await _context.DocumentEntetes.FirstOrDefaultAsync(d => d.Id == id && d.TypeDocumentCode == TypesDocument.VerifMachine);
            if (p is null) return false;

            if (statut == StatutsPlan.Archive && p.Statut == StatutsPlan.Brouillon)
            {
                return false;
            }

            p.Statut = statut;
        }
        else if (category == "ECH")
        {
            var p = await _context.DocumentEntetes.FirstOrDefaultAsync(d => d.Id == id && d.TypeDocumentCode == TypesDocument.PlanEchantillonnage);
            if (p is null) return false;

            if (statut == StatutsPlan.Archive && p.Statut == StatutsPlan.Brouillon)
            {
                return false;
            }

            p.Statut = statut;
        }
        else
        {
            var p = await _context.DocumentEntetes.FindAsync(id);
            if (p is null) return false;

            if (statut == StatutsPlan.Archive && p.Statut == StatutsPlan.Brouillon)
            {
                return false;
            }

            p.Statut = statut;
        }

        await VerifierReactivationsAsync();
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SupprimerBrouillonPlanAsync(string category, Guid id)
    {
        category = category?.ToUpperInvariant() ?? "";
        // FAB : plans de fabrication stockés dans Document_* → traités par la branche générique.
        if (category == "VM")
        {
            var plan = await _context.DocumentEntetes
                .Include(v => v.DocumentLignes)
                    .ThenInclude(l => l.DocumentLigneExtraColonnes)
                .Include(v => v.DocumentVerifMachineFamilles)
                .FirstOrDefaultAsync(p => p.Id == id && p.TypeDocumentCode == TypesDocument.VerifMachine);

            if (plan is null) return false;
            if (plan.Statut != StatutsPlan.Brouillon) return false;

            foreach (var ligne in plan.DocumentLignes)
            {
                _context.Set<DocumentLigneExtraColonne>().RemoveRange(ligne.DocumentLigneExtraColonnes);
            }
            _context.DocumentLignes.RemoveRange(plan.DocumentLignes);
            _context.DocumentVerifMachineFamilles.RemoveRange(plan.DocumentVerifMachineFamilles);
            _context.DocumentEntetes.Remove(plan);

            await _context.SaveChangesAsync();
            return true;
        }
        else if (category == "ECH")
        {
            var plan = await _context.DocumentEntetes
                .FirstOrDefaultAsync(p => p.Id == id && p.TypeDocumentCode == TypesDocument.PlanEchantillonnage);

            if (plan is null) return false;
            if (plan.Statut != StatutsPlan.Brouillon) return false;

            _context.DocumentEntetes.Remove(plan);

            await _context.SaveChangesAsync();
            return true;
        }
        else
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
}
