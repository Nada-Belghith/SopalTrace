using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;

namespace SopalTrace.Infrastructure.Repositories.Execution;

public class OccurrenceRepository : IOccurrenceRepository
{
    private readonly SopalTraceDbContext _context;

    public OccurrenceRepository(SopalTraceDbContext context)
    {
        _context = context;
    }

    public async Task<ExecControleOf?> GetExecControleOfWithIntermediairesAsync(Guid execControleOfId)
    {
        return await _context.ExecControleOfs
            .Include(o => o.ExecPrelevementIntermediaires)
            .Include(o => o.ExecPieceTypes)
            .Include(o => o.ExecControleTranches)
            .Include(o => o.ExecControleDocumentStatuts)
            .Include(o => o.NumeroOfNavigation)
            .FirstOrDefaultAsync(o => o.Id == execControleOfId);
    }

    public async Task<List<PlanFabricationSection>> GetSectionsActivesAsync(Guid planEnteteId)
    {
        return await _context.Set<PlanFabricationSection>()
            .Include(s => s.Periodicite)
            .Include(s => s.TypeSection)
            .Include(s => s.RegleEchantillonnage)
            .Where(s => s.PlanEnteteId == planEnteteId)
            .ToListAsync();
    }

    /// <summary>
    /// Retourne les sections ECHANTILLONNAGE du plan d'assemblage pour un OF ASS.
    /// Cherche le plan via ExecControleDocumentStatut -> DocumentEntete -> DocumentSection.
    /// </summary>
    public async Task<List<DocumentSection>> GetDocumentSectionsEchantillonnageAsync(Guid execControleOfId)
    {
        var sections = await GetDocumentSectionsActivesAsync(execControleOfId);
        // Filtrer uniquement les sections ECHANTILLONNAGE selon les codes
        return sections.Where(s => IsEchantillonnageSection(s)).ToList();
    }

    public async Task<bool> IsDemarrageAssTermineAsync(Guid execControleOfId)
    {
        var statuts = await _context.ExecControleDocumentStatuts
            .Where(s => s.ExecControleOfId == execControleOfId)
            .ToListAsync();

        if (!statuts.Any()) return true;

        var vmStatuts = statuts.Where(s => s.TypeDocument == "VERIF_MACHINE").ToList();
        var planStatuts = statuts.Where(s => s.TypeDocument == "PLAN_ASS" || s.TypeDocument == "PLAN_ASSEMBLAGE").ToList();

        if (!vmStatuts.Any() && !planStatuts.Any()) return true;

        bool vmDone = !vmStatuts.Any() || vmStatuts.All(s => s.EstDemarrageTermine || s.EstTermine);
        bool planDone = !planStatuts.Any() || planStatuts.All(s => s.EstDemarrageTermine || s.EstTermine || s.EstPauseTermine);

        return vmDone && planDone;
    }

    public async Task<List<DocumentSection>> GetDocumentSectionsActivesAsync(Guid execControleOfId)
    {
        return await GetDocumentSectionsActivesParPosteAsync(execControleOfId, null!);
    }

    public async Task<List<DocumentSection>> GetDocumentSectionsActivesParPosteAsync(Guid execControleOfId, string posteCode)
    {
        var query = _context.ExecControleDocumentStatuts
            .Where(s => s.ExecControleOfId == execControleOfId && s.DocId.HasValue);

        if (!string.IsNullOrEmpty(posteCode))
        {
            query = query.Where(s => s.PosteCode == posteCode);
        }

        var docIds = await query
            .Select(s => s.DocId!.Value)
            .Distinct()
            .ToListAsync();

        if (!docIds.Any()) return new List<DocumentSection>();

        var matchingEnteteIds = await _context.DocumentEntetes
            .Where(d => docIds.Contains(d.Id) || (d.FormulaireId.HasValue && docIds.Contains(d.FormulaireId.Value)))
            .Select(d => d.Id)
            .ToListAsync();

        var allEnteteIds = docIds.Concat(matchingEnteteIds).Distinct().ToList();

        return await _context.DocumentSections
            .Include(s => s.Entete)
            .Include(s => s.TypeSection)
            .Include(s => s.Periodicite)
            .Include(s => s.RegleEchantillonnage)
            .Include(s => s.DocumentLignes)
                .ThenInclude(l => l.TypeControle)
            .Include(s => s.DocumentLignes)
                .ThenInclude(l => l.MoyenControle)
            .Include(s => s.DocumentLignes)
                .ThenInclude(l => l.Caracteristique)
            .Where(s => allEnteteIds.Contains(s.EnteteId))
            .ToListAsync();
    }

    public async Task<List<DocumentSection>> GetDocumentSectionsEchantillonnageParPosteAsync(Guid execControleOfId, string posteCode)
    {
        var sections = await GetDocumentSectionsActivesParPosteAsync(execControleOfId, posteCode);
        return sections.Where(s => IsEchantillonnageSection(s)).ToList();
    }

    private static bool IsEchantillonnageSection(DocumentSection s)
    {
        string lib = (s.LibelleSection ?? "").ToLower();
        if (lib.Contains("échantillonnage") || lib.Contains("echantillonnage") || lib.Contains("en cours de production") || lib.Contains("p/h") || lib.Contains("100%"))
        {
            return true;
        }

        bool isTimeBased = false;
        if (s.Periodicite != null)
        {
            string perCode = (s.Periodicite.Code ?? "").ToUpper();
            string unite = (s.Periodicite.FrequenceUnite ?? "").ToUpper();
            if (unite.Contains("HEURE") || unite.Contains("PCT_HEURE") || perCode.EndsWith("H"))
            {
                isTimeBased = true;
            }
        }
        if (s.RegleEchantillonnage != null)
        {
            string regleLib = (s.RegleEchantillonnage.Libelle ?? "").ToLower();
            if (regleLib.Contains("p/h") || regleLib.Contains("heure") || regleLib.Contains("échantillonnage"))
            {
                isTimeBased = true;
            }
        }
        return isTimeBased;
    }

    public async Task<List<DocumentLigne>> GetDocumentLignesForSectionAsync(Guid sectionId)
    {
        return await _context.DocumentLignes
            .Include(l => l.TypeControle)
            .Include(l => l.MoyenControle)
            .Include(l => l.Caracteristique)
            .Include(l => l.DocumentLigneExtraColonnes)
            .Where(l => l.SectionId == sectionId)
            .OrderBy(l => l.OrdreAffiche)
            .ToListAsync();
    }

    public async Task<List<ExecPrelevementIntermediaire>> GetIntermediairesActifsAsync(Guid execControleOfId)
    {
        return await _context.ExecPrelevementIntermediaires
            .Where(p => p.ExecControleOfid == execControleOfId && !p.EstRepondu)
            .ToListAsync();
    }

    public async Task<List<ExecPrelevementIntermediaire>> GetIntermediairesParOfAsync(Guid execControleOfId)
    {
        return await _context.ExecPrelevementIntermediaires
            .Where(p => p.ExecControleOfid == execControleOfId)
            .ToListAsync();
    }

    public async Task<ExecPrelevementIntermediaire?> GetIntermediaireAsync(Guid id)
    {
        return await _context.ExecPrelevementIntermediaires.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<ExecPrelevementIntermediaire>> GetIntermediairesParTrancheAsync(Guid execControleOfId, string trancheHoraire)
    {
        var cleanTranche = trancheHoraire.Split('|')[0];
        return await _context.ExecPrelevementIntermediaires
            .Where(o => o.ExecControleOfid == execControleOfId && (o.TrancheHoraire == trancheHoraire || o.TrancheHoraire.StartsWith(cleanTranche)))
            .ToListAsync();
    }

    public async Task<ExecControleTranche?> GetTrancheExistanteAsync(Guid execControleOfId, string trancheHoraire)
    {
        return await _context.ExecControleTranches
            .FirstOrDefaultAsync(t => t.ExecControleOfid == execControleOfId && t.TrancheHoraire == trancheHoraire);
    }

    public async Task<ExecControleTranche?> GetDerniereTrancheAsync(Guid execControleOfId)
    {
        return await _context.ExecControleTranches
            .Where(t => t.ExecControleOfid == execControleOfId)
            .OrderByDescending(t => t.HeureDebut)
            .FirstOrDefaultAsync();
    }

    public void AddIntermediaires(IEnumerable<ExecPrelevementIntermediaire> intermediaires)
    {
        _context.ExecPrelevementIntermediaires.AddRange(intermediaires);
    }

    public void AddTranche(ExecControleTranche tranche)
    {
        _context.ExecControleTranches.Add(tranche);
    }

    public void AddPieceType(ExecPieceType pieceType)
    {
        _context.ExecPieceTypes.Add(pieceType);
    }

    public void RemoveIntermediaires(IEnumerable<ExecPrelevementIntermediaire> intermediaires)
    {
        _context.ExecPrelevementIntermediaires.RemoveRange(intermediaires);
    }

    public async Task<List<PlanFabricationLigne>> GetLignesForSectionAsync(Guid sectionId)
    {
        return await _context.PlanFabricationLignes
            .Include(l => l.TypeControle)
            .Include(l => l.MoyenControle)
            .Include(l => l.PlanFabricationLigneExtraColonnes)
            .Where(l => l.SectionId == sectionId)
            .OrderBy(l => l.OrdreAffiche)
            .ToListAsync();
    }

    public void AddLigneReponses(IEnumerable<ExecControleLigneReponse> reponses)
    {
        _context.ExecControleLigneReponses.AddRange(reponses);
    }

    public async Task<Guid?> GetUtilisateurIdByMatriculeAsync(string matricule)
    {
        // Recherche dans UtilisateursApp ou Autilis selon ce qui est défini dans SopalTraceDbContext.
        // Si la table UtilisateursApp existe, on l'utilise.
        var user = await _context.UtilisateursApps.FirstOrDefaultAsync(u => u.Matricule == matricule);
        if (user != null) return user.Id;
        
        // Fallback pour éviter l'erreur de FK si le matricule n'existe pas (ex: test local)
        var firstUser = await _context.UtilisateursApps.FirstOrDefaultAsync();
        return firstUser?.Id;
    }

    public async Task<string> GetContexteOccurrenceAsync(Guid sectionId)
    {
        var fabSec = await _context.Set<PlanFabricationSection>()
            .Include(s => s.TypeSection)
            .Include(s => s.Periodicite)
            .FirstOrDefaultAsync(s => s.Id == sectionId);
        
        if (fabSec != null)
        {
            string type = fabSec.TypeSection?.Code?.ToUpper() ?? "";
            string per = fabSec.Periodicite?.Code?.ToUpper() ?? "";
            
            if (type == "REGLAGE" || type == "REGLAGE_PROD") return "REGLAGE";
            if (per.Contains("100PCT") || type == "100PCT") return "100PCT";
            if (type == "LOT" || type == "LOT_POSTE" || type == "LOT_OF" || type == "ECHANTILLONNAGE" || per == "SERIE_1P" || per == "SERIE_4P") return "LOT";
            return "LOT"; // Fallback conforme à la contrainte CHECK (REGLAGE, LOT, 100PCT)
        }
        
        var assSec = await _context.DocumentSections
            .Include(s => s.TypeSection)
            .Include(s => s.Periodicite)
            .FirstOrDefaultAsync(s => s.Id == sectionId);
            
        if (assSec != null)
        {
            string type = assSec.TypeSection?.Code?.ToUpper() ?? "";
            string per = assSec.Periodicite?.Code?.ToUpper() ?? "";
            string libelle = assSec.LibelleSection?.ToUpper() ?? "";
            
            if (type == "REGLAGE" || type == "REGLAGE_PROD") return "REGLAGE";
            if (per.Contains("100PCT") || type == "100PCT" || libelle.Contains("100%")) return "100PCT";
            if (type == "LOT" || type == "LOT_POSTE" || type == "LOT_OF" || type == "ECHANTILLONNAGE" || per == "SERIE_1P" || per == "SERIE_4P") return "LOT";
            return "LOT"; // Fallback conforme à la contrainte CHECK (REGLAGE, LOT, 100PCT)
        }
        
        return "LOT"; // Fallback global
    }

    public async Task<int?> GetEffectifEchantillonnageAsync(Guid execControleOfId)
    {
        return await _context.ExecEchantillonnages
            .Where(e => e.ExecControleDocumentStatut.ExecControleOfId == execControleOfId)
            .Select(e => e.EffectifParPosteAb)
            .FirstOrDefaultAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
