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
        return await _context.Periodicites.FirstOrDefaultAsync(p => p.Libelle == libelle);
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
        await Task.CompletedTask;
    }
}
