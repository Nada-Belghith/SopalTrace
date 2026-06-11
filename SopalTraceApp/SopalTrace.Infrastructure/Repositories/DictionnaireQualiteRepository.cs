using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces;
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

    public async Task<Periodicite?> GetPeriodiciteByLibelleAsync(string libelle)
    {
        if (string.IsNullOrEmpty(libelle)) return null;
        var normalized = libelle.Trim().ToLower();
        return await _context.Periodicites
            .FirstOrDefaultAsync(p => p.Libelle.Trim().ToLower() == normalized);
    }

    public async Task<Periodicite?> GetPeriodiciteByCodeAsync(string code)
    {
        if (string.IsNullOrEmpty(code)) return null;
        var normalized = code.Trim().ToLower();
        return await _context.Periodicites
            .FirstOrDefaultAsync(p => p.Code.Trim().ToLower() == normalized);
    }

    public async Task AddPeriodiciteAsync(Periodicite entite)
    {
        _context.Periodicites.Add(entite);
        await Task.CompletedTask;
    }

    public async Task<TypeSection> GetTypeSectionByLibelleAsync(string libelle)
    {
        var normalized = libelle.Trim();
        // Recherche souple : ignore casse et espaces superflus
        return await _context.Set<TypeSection>().FirstOrDefaultAsync(t => 
            t.Libelle.Trim().ToLower() == normalized.ToLower());
    }

    public async Task AddTypeSectionAsync(TypeSection entite)
    {
        _context.Set<TypeSection>().Add(entite);
        await Task.CompletedTask;
    }

    public async Task<System.Collections.Generic.List<TypeSection>> GetAllTypeSectionsAsync()
    {
        return await _context.Set<TypeSection>().ToListAsync();
    }

    public async Task<TypeCaracteristique> GetTypeCaracteristiqueByLibelleAsync(string libelle)
    {
        var normalized = libelle.Trim().ToLower();
        return await _context.Set<TypeCaracteristique>().FirstOrDefaultAsync(t => t.Libelle.Trim().ToLower() == normalized);
    }

    public async Task AddTypeCaracteristiqueAsync(TypeCaracteristique entite)
    {
        _context.Set<TypeCaracteristique>().Add(entite);
        await Task.CompletedTask;
    }

    public async Task<TypeControle> GetTypeControleByLibelleAsync(string libelle)
    {
        var normalized = libelle.Trim().ToLower();
        return await _context.Set<TypeControle>().FirstOrDefaultAsync(t => t.Libelle.Trim().ToLower() == normalized);
    }

    public async Task AddTypeControleAsync(TypeControle entite)
    {
        _context.Set<TypeControle>().Add(entite);
        await Task.CompletedTask;
    }

    public async Task<MoyenControle> GetMoyenControleByLibelleAsync(string libelle)
    {
        var normalized = libelle.Trim().ToLower();
        return await _context.Set<MoyenControle>().FirstOrDefaultAsync(t => t.Libelle.Trim().ToLower() == normalized);
    }

    public async Task AddMoyenControleAsync(MoyenControle entite)
    {
        _context.Set<MoyenControle>().Add(entite);
        await Task.CompletedTask;
    }

    public async Task<Instrument> GetInstrumentByCodeAsync(string codeInstrument)
    {
        var normalizedCode = codeInstrument.Trim();
        return await _context.Set<Instrument>().FirstOrDefaultAsync(t => t.CodeInstrument == normalizedCode);
    }

    public async Task AddInstrumentAsync(Instrument entite)
    {
        _context.Set<Instrument>().Add(entite);
        await Task.CompletedTask;
    }

    public async Task<RefRegleEchantillonnage> GetRegleEchantillonnageByLibelleAsync(string libelle)
    {
        var normalized = libelle.Trim();
        return await _context.RefRegleEchantillonnages.FirstOrDefaultAsync(r => 
            r.Libelle.Trim().ToLower() == normalized.ToLower());
    }

    public async Task AddRegleEchantillonnageAsync(RefRegleEchantillonnage entite)
    {
        _context.RefRegleEchantillonnages.Add(entite);
        await Task.CompletedTask;
    }

    public async Task<PieceReference> GetPieceReferenceByCodeAsync(string code)
    {
        var normalizedCode = code.Trim().ToUpper();
        return await _context.PieceReferences.FirstOrDefaultAsync(p => p.Code == normalizedCode);
    }

    public async Task AddPieceReferenceAsync(PieceReference entite)
    {
        _context.PieceReferences.Add(entite);
        await Task.CompletedTask;
    }
    
    public async Task<RefFamilleCorp> GetFamilleCorpsByCodeAsync(string code)
    {
        var normalizedCode = code.Trim().ToUpper();
        return await _context.RefFamilleCorps.FirstOrDefaultAsync(f => f.Code == normalizedCode);
    }

    public async Task AddFamilleCorpsAsync(RefFamilleCorp entite)
    {
        _context.RefFamilleCorps.Add(entite);
        await Task.CompletedTask;
    }

    public async Task<RefMoyenDetection> GetMoyenDetectionByLibelleAsync(string libelle)
    {
        var normalized = libelle.Trim();
        return await _context.RefMoyenDetections.FirstOrDefaultAsync(m => m.Designation == normalized || m.Code == normalized);
    }

    public async Task AddMoyenDetectionAsync(RefMoyenDetection entite)
    {
        _context.RefMoyenDetections.Add(entite);
        await Task.CompletedTask;
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

    public async Task AddRisqueDefautAsync(RisqueDefaut entite)
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
    }
    public async Task<System.Collections.Generic.List<TypeRobinet>> GetActiveTypeRobinetsAsync() => await _context.TypeRobinets.Where(x => x.Actif).ToListAsync();
    public async Task<System.Collections.Generic.List<NatureArticle>> GetActiveNatureArticlesFabriqueAsync() => await _context.NatureArticles.Where(x => x.Actif && x.Origine == "FABRIQUE").ToListAsync();
    public async Task<System.Collections.Generic.List<Operation>> GetActiveOperationsAsync() => await _context.Operations.Where(x => x.Actif).ToListAsync();
    public async Task<System.Collections.Generic.List<NatureArticleOperation>> GetAllNatureArticleOperationsAsync() => await _context.NatureArticleOperations.ToListAsync();
    public async Task<System.Collections.Generic.List<TypeControle>> GetActiveTypeControlesAsync() => await _context.TypeControles.Where(x => x.Actif).ToListAsync();
    public async Task<System.Collections.Generic.List<TypeCaracteristique>> GetActiveTypeCaracteristiquesAsync() => await _context.TypeCaracteristiques.Where(x => x.Actif).ToListAsync();
    public async Task<System.Collections.Generic.List<MoyenControle>> GetActiveMoyenControlesAsync() => await _context.MoyenControles.Where(x => x.Actif).ToListAsync();
    public async Task<System.Collections.Generic.List<PosteTravail>> GetActivePosteTravailsAsync() => await _context.PosteTravails.Where(x => x.Actif).ToListAsync();
    public async Task<System.Collections.Generic.List<Periodicite>> GetAllPeriodicitesAsync() => await _context.Periodicites.ToListAsync();
    public async Task<System.Collections.Generic.List<Instrument>> GetActiveInstrumentsAsync() => await _context.Instruments.Where(x => x.Actif).ToListAsync();
    public async Task<System.Collections.Generic.List<Nqa>> GetActiveNqasAsync() => await _context.Nqas.ToListAsync();
    public async Task<System.Collections.Generic.List<Defautheque>> GetActiveDefauthequesAsync() => await _context.Defautheques.Where(x => x.Actif).ToListAsync();
    public async Task<System.Collections.Generic.List<RefRegleEchantillonnage>> GetActiveRegleEchantillonnagesAsync() => await _context.RefRegleEchantillonnages.Where(x => x.Actif).ToListAsync();
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
        var typeRobinet = await _context.ProduitFinis
            .Where(x => x.CodeArticle == codeNormalise)
            .Select(x => x.TypeRobinetCode)
            .FirstOrDefaultAsync();

        if (string.IsNullOrEmpty(typeRobinet))
        {
            typeRobinet = await _context.BomdNomenclatures
                .Where(n => n.CodeComposant == codeNormalise)
                .Join(_context.ProduitFinis,
                      n => n.ArticleParent,
                      p => p.CodeArticle,
                      (n, p) => p.TypeRobinetCode)
                .FirstOrDefaultAsync();
        }

        return typeRobinet;
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
}
