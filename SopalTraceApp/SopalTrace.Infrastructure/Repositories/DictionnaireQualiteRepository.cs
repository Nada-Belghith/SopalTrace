using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;

namespace SopalTrace.Infrastructure.Repositories;

public class DictionnaireQualiteRepository : IDictionnaireQualiteRepository
{
    private readonly SopalTraceDbContext _context;

    public DictionnaireQualiteRepository(SopalTraceDbContext context)
    {
        _context = context;
    }



    public Task AddPeriodiciteAsync(Periodicite entite)
    {
        _context.Periodicites.Add(entite);
        return Task.CompletedTask;
    }

    public async Task<PeriodiciteMachine?> GetPeriodiciteMachineByLibelleAsync(string libelle)
    {
        return await _context.PeriodiciteMachines
            .FirstOrDefaultAsync(p => p.Libelle.ToLower() == libelle.ToLower());
    }

    public async Task<PeriodiciteMachine?> GetPeriodiciteMachineByCodeAsync(string code)
    {
        return await _context.PeriodiciteMachines
            .FirstOrDefaultAsync(p => p.Code == code);
    }

    public Task AddPeriodiciteMachineAsync(PeriodiciteMachine entite)
    {
        _context.PeriodiciteMachines.Add(entite);
        return Task.CompletedTask;
    }

    public async Task<TypeSection?> GetTypeSectionByLibelleAsync(string libelle)
    {
        var normalized = libelle.Trim();
        // Recherche souple : ignore casse et espaces superflus
        return await _context.Set<TypeSection>().FirstOrDefaultAsync(t => 
            t.Libelle.Trim().ToLower() == normalized.ToLower());
    }

    public Task AddTypeSectionAsync(TypeSection entite)
    {
        _context.Set<TypeSection>().Add(entite);
        return Task.CompletedTask;
    }

    public async Task<System.Collections.Generic.List<TypeSection>> GetAllTypeSectionsAsync()
    {
        return await _context.Set<TypeSection>().ToListAsync();
    }

    public async Task<TypeCaracteristique?> GetTypeCaracteristiqueByLibelleAsync(string libelle)
    {
        var normalized = libelle.Trim().ToLower();
        return await _context.Set<TypeCaracteristique>().FirstOrDefaultAsync(t => t.Libelle.Trim().ToLower() == normalized);
    }

    public Task AddTypeCaracteristiqueAsync(TypeCaracteristique entite)
    {
        _context.Set<TypeCaracteristique>().Add(entite);
        return Task.CompletedTask;
    }

    public async Task<TypeControle?> GetTypeControleByLibelleAsync(string libelle)
    {
        var normalized = libelle.Trim().ToLower();
        return await _context.Set<TypeControle>().FirstOrDefaultAsync(t => t.Libelle.Trim().ToLower() == normalized);
    }

    public Task AddTypeControleAsync(TypeControle entite)
    {
        _context.Set<TypeControle>().Add(entite);
        return Task.CompletedTask;
    }

    public async Task<MoyenControle?> GetMoyenControleByLibelleAsync(string libelle)
    {
        var normalized = libelle.Trim().ToLower();
        return await _context.Set<MoyenControle>().FirstOrDefaultAsync(t => t.Libelle.Trim().ToLower() == normalized);
    }

    public Task AddMoyenControleAsync(MoyenControle entite)
    {
        _context.Set<MoyenControle>().Add(entite);
        return Task.CompletedTask;
    }

    public async Task<Instrument?> GetInstrumentByCodeAsync(string codeInstrument)
    {
        var normalizedCode = codeInstrument.Trim();
        return await _context.Set<Instrument>().FirstOrDefaultAsync(t => t.CodeInstrument == normalizedCode);
    }

    public Task AddInstrumentAsync(Instrument entite)
    {
        _context.Set<Instrument>().Add(entite);
        return Task.CompletedTask;
    }

    public async Task<PieceReference?> GetPieceReferenceByCodeAsync(string code)
    {
        var normalizedCode = code.Trim().ToUpper();
        return await _context.PieceReferences.FirstOrDefaultAsync(p => p.Code == normalizedCode);
    }

    public Task AddPieceReferenceAsync(PieceReference entite)
    {
        _context.PieceReferences.Add(entite);
        return Task.CompletedTask;
    }
    
    public async Task<RefFamilleCorp?> GetFamilleCorpsByCodeAsync(string code)
    {
        var normalizedCode = code.Trim().ToUpper();
        return await _context.RefFamilleCorps.FirstOrDefaultAsync(f => f.Code == normalizedCode);
    }

    public Task AddFamilleCorpsAsync(RefFamilleCorp entite)
    {
        _context.RefFamilleCorps.Add(entite);
        return Task.CompletedTask;
    }

    public async Task<RefMoyenDetection?> GetMoyenDetectionByLibelleAsync(string libelle)
    {
        var normalized = libelle.Trim();
        return await _context.RefMoyenDetections.FirstOrDefaultAsync(m => m.Designation == normalized || m.Code == normalized);
    }

    public Task AddMoyenDetectionAsync(RefMoyenDetection entite)
    {
        _context.RefMoyenDetections.Add(entite);
        return Task.CompletedTask;
    }

    public async Task<RisqueDefaut?> GetRisqueDefautByLibelleAsync(string libelle)
    {
        if (string.IsNullOrWhiteSpace(libelle)) return null;
        var normalized = libelle.Trim().ToLowerInvariant();
        
        // 1. Vérifier dans le tracker local (mémoire) avec une comparaison robuste
        var local = _context.RisqueDefauts.Local
            .FirstOrDefault(r => r.LibelleDefaut != null && r.LibelleDefaut.Trim().ToLowerInvariant() == normalized);
        
        if (local != null) return local;

        // 2. Sinon chercher en base
        return await _context.RisqueDefauts.FirstOrDefaultAsync(r => 
            r.LibelleDefaut.Trim().ToLower() == normalized);
    }

    public async Task<RisqueDefaut?> GetRisqueDefautByCodeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        var normalized = code.Trim().ToUpperInvariant();
        
        var local = _context.RisqueDefauts.Local
            .FirstOrDefault(r => r.CodeDefaut != null && r.CodeDefaut.Trim().ToUpperInvariant() == normalized);
            
        if (local != null) return local;

        return await _context.RisqueDefauts.FirstOrDefaultAsync(r => r.CodeDefaut == normalized);
    }

    public Task AddRisqueDefautAsync(RisqueDefaut entite)
    {
        // On vérifie juste si on ne l'a pas déjà ajouté dans cette même requête
        var normalizedLibelle = entite.LibelleDefaut.Trim().ToLowerInvariant();
        var normalizedCode = entite.CodeDefaut.Trim().ToUpperInvariant();

        var alreadyInContext = _context.RisqueDefauts.Local.Any(r => 
            (r.LibelleDefaut != null && r.LibelleDefaut.Trim().ToLowerInvariant() == normalizedLibelle) ||
            (r.CodeDefaut != null && r.CodeDefaut.Trim().ToUpperInvariant() == normalizedCode));

        if (!alreadyInContext)
        {
            _context.RisqueDefauts.Add(entite);
        }
        return Task.CompletedTask;
    }
    public async Task<System.Collections.Generic.List<TypeRobinet>> GetActiveTypeRobinetsAsync() => await _context.TypeRobinets.Where(x => x.Actif).ToListAsync();
    // Natures fabriquées : celles qui regroupent au moins un article actif fabriqué (PF ou SF).
    // L'origine est portée par l'article (Article.Origin), une nature peut mêler fabriqué et acheté.
    public async Task<System.Collections.Generic.List<NatureArticle>> GetActiveNatureArticlesFabriqueAsync() => await _context.NatureArticles
        .Where(x => x.Actif && x.Articles.Any(a => a.Actif && a.Origin == ArticleOrigins.Manufactured)).ToListAsync();
    public async Task<System.Collections.Generic.List<Operation>> GetActiveOperationsAsync() => await _context.Operations.Where(x => x.Actif).ToListAsync();
    public async Task<System.Collections.Generic.List<NatureArticleOperation>> GetAllNatureArticleOperationsAsync() => await _context.NatureArticleOperations.ToListAsync();
    public async Task<System.Collections.Generic.List<TypeControle>> GetActiveTypeControlesAsync() => await _context.TypeControles.Where(x => x.Actif).ToListAsync();
    public async Task<System.Collections.Generic.List<TypeCaracteristique>> GetActiveTypeCaracteristiquesAsync() => await _context.TypeCaracteristiques.Where(x => x.Actif).ToListAsync();
    public async Task<System.Collections.Generic.List<MoyenControle>> GetActiveMoyenControlesAsync() => await _context.MoyenControles.Where(x => x.Actif).ToListAsync();
    public async Task<System.Collections.Generic.List<PosteTravail>> GetActivePosteTravailsAsync() => await _context.PosteTravails.Where(x => x.Actif).ToListAsync();
    public async Task<System.Collections.Generic.List<Periodicite>> GetAllPeriodicitesAsync() => await _context.Periodicites.ToListAsync();
    public async Task<System.Collections.Generic.List<PeriodiciteMachine>> GetAllPeriodicitesMachineAsync() => await _context.PeriodiciteMachines.ToListAsync();
    public async Task<System.Collections.Generic.List<Instrument>> GetActiveInstrumentsAsync() => await _context.Instruments.Where(x => x.Actif).ToListAsync();
    public async Task<System.Collections.Generic.List<Instrument>> GetInstrumentsByCodesAsync(System.Collections.Generic.List<string> codes) => await _context.Instruments.Where(x => codes.Contains(x.CodeInstrument)).ToListAsync();
    public async Task<System.Collections.Generic.List<Nqa>> GetActiveNqasAsync() => await _context.Nqas.ToListAsync();
    public async Task AddNqaAsync(Nqa entite) => await _context.Nqas.AddAsync(entite);
    public async Task<System.Collections.Generic.List<FamilleProduitFini>> GetActiveFamilleProduitFinisAsync() => await _context.FamilleProduitFinis.Where(x => x.Actif).ToListAsync();
    public async Task<System.Collections.Generic.List<Machine>> GetAllMachinesAsync() => await _context.Machines.ToListAsync();
    public async Task<System.Collections.Generic.List<Machine>> GetActiveMachinesAsync() => await _context.Machines.Where(x => x.Actif).ToListAsync();
    public async Task<System.Collections.Generic.List<RisqueDefaut>> GetAllRisqueDefautsAsync() => await _context.RisqueDefauts.ToListAsync();
    public async Task<System.Collections.Generic.List<PieceReference>> GetActivePieceReferencesAsync() => await _context.PieceReferences.Where(x => x.Actif).ToListAsync();
    public async Task<System.Collections.Generic.List<RefFamilleCorp>> GetAllFamilleCorpsAsync() => await _context.RefFamilleCorps.ToListAsync();
    public async Task<System.Collections.Generic.List<RefMoyenDetection>> GetAllMoyenDetectionsAsync() => await _context.RefMoyenDetections.ToListAsync();
    public async Task<Article?> GetArticleByCodeNormaliseAsync(string codeNormalise) => await _context.Articles.FirstOrDefaultAsync(x => x.CodeArticle == codeNormalise);
    public async Task<string?> GetTypeRobinetCodeForArticleAsync(string codeNormalise)
    {
        // Le type de robinet est porté par la famille du produit fini.
        var famille = await GetFamilleProduitCodeForArticleAsync(codeNormalise);
        if (famille == null) return null;

        return await _context.FamilleProduitFinis
            .Where(f => f.Code == famille)
            .Select(f => f.TypeRobinetCode)
            .FirstOrDefaultAsync();
    }

    public async Task<string?> GetFamilleProduitCodeForArticleAsync(string codeNormalise)
    {
        // Article produit fini : sa propre famille.
        var familleDirecte = await _context.ProduitFinis
            .Where(p => p.CodeArticle == codeNormalise)
            .Select(p => p.FamilleProduitFiniCode)
            .FirstOrDefaultAsync();

        if (!string.IsNullOrEmpty(familleDirecte))
            return familleDirecte;

        // Composant : famille du produit fini qui l'utilise (nomenclature).
        return await _context.BomdNomenclatures
            .Where(n => n.CodeComposant == codeNormalise)
            .Join(_context.ProduitFinis,
                  n => n.ArticleParent,
                  p => p.CodeArticle,
                  (n, p) => p.FamilleProduitFiniCode)
            .FirstOrDefaultAsync();
    }

    public async Task<System.Collections.Generic.IReadOnlyList<Article>> SearchArticlesSfAsync(string query, int maxResults = 15)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new System.Collections.Generic.List<Article>();

        var q = query.Trim().ToUpperInvariant();

        return await _context.Articles
            .Where(a => a.CodeArticle.Contains(q) && a.NatureArticleCode != "PISTON" && a.NatureArticleCode != "PF")
            .Take(maxResults)
            .ToListAsync();
    }

    public async Task<string?> GetFamilleArticleLibelleAsync(string? codeArticle, string? numeroOf = null)
    {
        if (string.IsNullOrWhiteSpace(codeArticle) || codeArticle == "Inconnu")
        {
            if (!string.IsNullOrWhiteSpace(numeroOf))
            {
                var mfg = await _context.MfgheadOrdreFabrications.FirstOrDefaultAsync(m => m.NumeroOf == numeroOf);
                if (mfg != null && !string.IsNullOrWhiteSpace(mfg.CodeArticle))
                {
                    codeArticle = mfg.CodeArticle;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(codeArticle) || codeArticle == "Inconnu")
            return null;

        var codeNormalise = codeArticle.Trim();

        // 1. Chercher dans ProduitFini
        var pf = await _context.ProduitFinis
            .Include(p => p.FamilleProduitFiniCodeNavigation)
            .FirstOrDefaultAsync(p => p.CodeArticle == codeNormalise);

        if (pf != null)
        {
            if (pf.FamilleProduitFiniCodeNavigation != null && !string.IsNullOrWhiteSpace(pf.FamilleProduitFiniCodeNavigation.Designation))
            {
                return $"{pf.FamilleProduitFiniCode} — {pf.FamilleProduitFiniCodeNavigation.Designation}";
            }
            if (!string.IsNullOrWhiteSpace(pf.FamilleProduitFiniCode))
            {
                return pf.FamilleProduitFiniCode;
            }
        }

        return null;
    }
}
