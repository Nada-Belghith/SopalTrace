using SopalTrace.Domain.Constants;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Infrastructure.Repositories;

public class RefFormulaireRepository : IRefFormulaireRepository
{
    private readonly SopalTraceDbContext _context;

    public RefFormulaireRepository(SopalTraceDbContext context)
    {
        _context = context;
    }

    public async Task<RefFormulaire?> GetByIdAsync(Guid id)
    {
        return await _context.RefFormulaires
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public Task UpdateAsync(RefFormulaire entity)
    {
        if (_context.Entry(entity).State == EntityState.Detached)
        {
            _context.RefFormulaires.Update(entity);
        }
        return Task.CompletedTask;
    }

    public async Task AddAsync(RefFormulaire entity)
    {
        await _context.RefFormulaires.AddAsync(entity);
    }

    public async Task<RefFormulaire?> GetFormulaireActifByRoleAsync(string role)
    {
        var roleTrimmed = role?.Trim();
        // Priorité : ACTIF d'abord, puis BROUILLON si pas encore activé
        return await _context.RefFormulaires
            .AsNoTracking()
            .Where(f => (f.Role != null && f.Role.Trim() == roleTrimmed)
                     && (f.Statut != null && (f.Statut.Trim() == StatutsPlan.Actif || f.Statut.Trim() == StatutsPlan.Brouillon)))
            .OrderBy(f => f.Statut!.Trim() == StatutsPlan.Actif ? 0 : 1) // ACTIF en premier
            .ThenByDescending(f => f.Version)
            .FirstOrDefaultAsync();
    }

    public async Task<RefFormulaire?> GetFormulaireActifByCodeReferenceAsync(string codeReference)
    {
        var codeRefTrimmed = codeReference?.Trim();
        // Priorité : ACTIF d'abord, puis BROUILLON si pas encore activé
        return await _context.RefFormulaires
            .AsNoTracking()
            .Where(f => (f.CodeReference != null && f.CodeReference.Trim() == codeRefTrimmed)
                     && (f.Statut != null && (f.Statut.Trim() == StatutsPlan.Actif || f.Statut.Trim() == StatutsPlan.Brouillon)))
            .OrderBy(f => f.Statut!.Trim() == StatutsPlan.Actif ? 0 : 1) // ACTIF en premier
            .ThenByDescending(f => f.Version)
            .FirstOrDefaultAsync();
    }

    public async Task<System.Collections.Generic.IEnumerable<RefFormulaire>> GetFormulairesByRoleAsync(string role)
    {
        var roleTrimmed = role?.Trim();
        var formulaires = await _context.RefFormulaires
            .AsNoTracking()
            .Where(f => (f.Role != null && f.Role.Trim() == roleTrimmed) && (f.Statut != null && (f.Statut.Trim() == StatutsPlan.Actif || f.Statut.Trim() == StatutsPlan.Brouillon)))
            .ToListAsync();

        // Une seule entrée par codeReference : priorité ACTIF, puis version la plus élevée
        return formulaires
            .GroupBy(f => f.CodeReference?.Trim() ?? string.Empty)
            .Select(g => g
                .OrderBy(f => f.Statut!.Trim() == StatutsPlan.Actif ? 0 : 1)
                .ThenByDescending(f => f.Version)
                .First())
            .OrderBy(f => f.Designation)
            .ToList();
    }

    public async Task<int> GetMaxVersionByCodeReferenceAsync(string codeReference)
    {
        var codeRefTrimmed = codeReference?.Trim();
        return await _context.RefFormulaires
            .Where(f => f.CodeReference != null && f.CodeReference.Trim() == codeRefTrimmed)
            .MaxAsync(f => (int?)f.Version) ?? 0;
    }

    public async Task UpdateStatutAsync(Guid formulaireId, string statut)
    {
        await _context.RefFormulaires
            .Where(f => f.Id == formulaireId)
            .ExecuteUpdateAsync(s => s.SetProperty(f => f.Statut, statut));
    }

    public async Task<System.Collections.Generic.List<RefFormulaireColonneDef>> GetColonnesActivesByCodeReferenceAsync(string codeReference)
    {
        return await _context.RefFormulaireColonneDefs
            .AsNoTracking()
            .Where(c => c.CodeReference == codeReference && c.Actif)
            .ToListAsync();
    }

    public async Task SyncColonnesAsync(string codeReference, System.Collections.Generic.List<SopalTrace.Application.Helpers.ColonneJsonDto> parsedCols)
    {
        var existingCols = await _context.RefFormulaireColonneDefs
            .Where(c => c.CodeReference == codeReference)
            .ToListAsync();

        var parsedKeys = parsedCols.Select(p => p.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existingByKey = existingCols.ToDictionary(c => c.CleColonne, StringComparer.OrdinalIgnoreCase);

        // 1. Soft-delete
        var idsToSoftDelete = existingCols
            .Where(c => !parsedKeys.Contains(c.CleColonne) && c.Actif)
            .Select(c => c.Id)
            .ToList();

        if (idsToSoftDelete.Any())
        {
            await _context.RefFormulaireColonneDefs
                .Where(c => idsToSoftDelete.Contains(c.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.Actif, false));
        }

        // 2. Colonnes à insérer (nouvelles)
        var newCols = parsedCols
            .Where(p => !string.IsNullOrWhiteSpace(p.Key)
                     && !string.IsNullOrWhiteSpace(p.Label)
                     && !existingByKey.ContainsKey(p.Key))
            .Select(p => new RefFormulaireColonneDef
            {
                Id = Guid.NewGuid(),
                CodeReference = codeReference,
                CleColonne = p.Key,
                LabelAffiche = p.Label,
                TypeValeur = string.IsNullOrWhiteSpace(p.Type) ? "TEXT" : p.Type,
                InsertAfter = p.InsertAfter,
                TargetTable = p.TargetTable,
                Actif = true
            })
            .ToList();

        if (newCols.Any())
        {
            _context.RefFormulaireColonneDefs.AddRange(newCols);
            await _context.SaveChangesAsync();
        }

        // 3. Mise à jour des colonnes existantes
        foreach (var p in parsedCols.Where(p => !string.IsNullOrWhiteSpace(p.Key) && existingByKey.ContainsKey(p.Key)))
        {
            var existing = existingByKey[p.Key];
            var label = p.Label;
            var type = string.IsNullOrWhiteSpace(p.Type) ? "TEXT" : p.Type;
            var insertAfter = p.InsertAfter;
            var targetTable = p.TargetTable;

            // Update only if something changed or it was deactivated
            if (existing.LabelAffiche != label || existing.TypeValeur != type || existing.InsertAfter != insertAfter || existing.TargetTable != targetTable || !existing.Actif)
            {
                await _context.RefFormulaireColonneDefs
                    .Where(c => c.Id == existing.Id)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(c => c.LabelAffiche, label)
                        .SetProperty(c => c.TypeValeur, type)
                        .SetProperty(c => c.InsertAfter, insertAfter)
                        .SetProperty(c => c.TargetTable, targetTable)
                        .SetProperty(c => c.Actif, true));
            }
        }
    }

    public async Task<System.Collections.Generic.List<RefFormulaireEquipe>> GetEquipesActivesByCodeReferenceAsync(string codeReference)
    {
        return await _context.RefFormulaireEquipes
            .AsNoTracking()
            .Where(e => e.CodeReference == codeReference && e.Actif)
            .OrderBy(e => e.OrdreAffiche)
            .ToListAsync();
    }

    public async Task SyncEquipesAsync(string codeReference, System.Collections.Generic.List<SopalTrace.Application.Helpers.EquipeJsonDto> parsedEquipes)
    {
        var existingEquipes = await _context.RefFormulaireEquipes
            .Where(e => e.CodeReference == codeReference)
            .ToListAsync();

        var parsedNames = parsedEquipes.Select(p => p.Nom).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existingByName = existingEquipes.ToDictionary(e => e.NomEquipe, StringComparer.OrdinalIgnoreCase);

        // 1. Soft-delete
        var idsToSoftDelete = existingEquipes
            .Where(e => !parsedNames.Contains(e.NomEquipe) && e.Actif)
            .Select(e => e.Id)
            .ToList();

        if (idsToSoftDelete.Any())
        {
            await _context.RefFormulaireEquipes
                .Where(e => idsToSoftDelete.Contains(e.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.Actif, false));
        }

        // 2. Insert new
        var newEquipes = parsedEquipes
            .Where(p => !string.IsNullOrWhiteSpace(p.Nom) && !existingByName.ContainsKey(p.Nom))
            .Select((p, idx) => new RefFormulaireEquipe
            {
                Id = Guid.NewGuid(),
                CodeReference = codeReference,
                NomEquipe = p.Nom,
                HeureDebut = p.Debut,
                HeureFin = p.Fin,
                OrdreAffiche = idx,
                Actif = true
            })
            .ToList();

        if (newEquipes.Any())
        {
            _context.RefFormulaireEquipes.AddRange(newEquipes);
            await _context.SaveChangesAsync();
        }

        // 3. Update existing
        int index = 0;
        foreach (var p in parsedEquipes.Where(p => !string.IsNullOrWhiteSpace(p.Nom) && existingByName.ContainsKey(p.Nom)))
        {
            var existing = existingByName[p.Nom];
            if (existing.HeureDebut != p.Debut || existing.HeureFin != p.Fin || existing.OrdreAffiche != index || !existing.Actif)
            {
                await _context.RefFormulaireEquipes
                    .Where(e => e.Id == existing.Id)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(e => e.HeureDebut, p.Debut)
                        .SetProperty(e => e.HeureFin, p.Fin)
                        .SetProperty(e => e.OrdreAffiche, index)
                        .SetProperty(e => e.Actif, true));
            }
            index++;
        }
    }
}
