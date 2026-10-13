using SopalTrace.Domain.Constants;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
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
        return await _context.RefFormulaires
            .AsNoTracking()
            .Where(f => (f.Role != null && f.Role.Trim() == roleTrimmed)
                     && (f.Statut != null && (f.Statut.Trim() == StatutsPlan.Actif || f.Statut.Trim() == StatutsPlan.Brouillon)))
            .OrderBy(f => f.Statut!.Trim() == StatutsPlan.Actif ? 0 : 1)
            .ThenByDescending(f => f.Version)
            .FirstOrDefaultAsync();
    }

    public async Task<RefFormulaire?> GetFormulaireActifByCodeReferenceAsync(string codeReference)
    {
        var codeRefTrimmed = codeReference?.Trim();
        return await _context.RefFormulaires
            .AsNoTracking()
            .Where(f => (f.CodeReference != null && f.CodeReference.Trim() == codeRefTrimmed)
                     && (f.Statut != null && (f.Statut.Trim() == StatutsPlan.Actif || f.Statut.Trim() == StatutsPlan.Brouillon)))
            .OrderBy(f => f.Statut!.Trim() == StatutsPlan.Actif ? 0 : 1)
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

    public async Task<System.Collections.Generic.List<RefFormulaireColonneDef>> GetColonnesActivesByFormulaireIdAsync(Guid formulaireId)
    {
        return await _context.RefFormulaireColonneDefs
            .AsNoTracking()
            .Include(c => c.ColumnAllowedValues)
            .Where(c => c.FormulaireId == formulaireId && c.Actif)
            .ToListAsync();
    }

    public async Task SyncColonnesAsync(Guid formulaireId, System.Collections.Generic.List<SopalTrace.Application.Helpers.ColonneJsonDto> parsedCols)
    {
        // AsNoTracking() is critical here: this method can be called from within a larger
        // unit-of-work (e.g. MettreAJourDocumentAsync) that has already loaded other tracked
        // entities. Loading these with tracking would pollute the change tracker and cause
        // DbUpdateConcurrencyException on the outer CommitAsync().
        var existingCols = await _context.RefFormulaireColonneDefs
            .AsNoTracking()
            .Where(c => c.FormulaireId == formulaireId)
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
                FormulaireId = formulaireId,
                CleColonne = p.Key,
                LabelAffiche = p.Label,
                TypeValeur = TypesValeurColonne.Normaliser(p.Type),
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
            var type = TypesValeurColonne.Normaliser(p.Type);
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

        // 4. Valeurs proposées des colonnes de type liste
        await SyncValeursProposeesAsync(formulaireId, parsedCols);
    }

    /// <summary>
    /// Aligne les valeurs proposées des colonnes LIST sur celles reçues : les nouvelles sont ajoutées,
    /// les absentes désactivées (jamais supprimées, des lignes peuvent encore les utiliser).
    /// </summary>
    private async Task SyncValeursProposeesAsync(Guid formulaireId, System.Collections.Generic.List<SopalTrace.Application.Helpers.ColonneJsonDto> parsedCols)
    {
        // Sans suivi, comme le reste de la synchronisation : ne pas polluer l'unité de travail appelante.
        var colonnesListe = await _context.RefFormulaireColonneDefs
            .AsNoTracking()
            .Include(c => c.ColumnAllowedValues)
            .Where(c => c.FormulaireId == formulaireId && c.TypeValeur == TypesValeurColonne.Liste)
            .ToListAsync();

        var nouvelles = new System.Collections.Generic.List<ColumnAllowedValue>();
        foreach (var colonne in colonnesListe)
        {
            var recue = parsedCols.FirstOrDefault(p => string.Equals(p.Key, colonne.CleColonne, StringComparison.OrdinalIgnoreCase));
            if (recue == null) continue;

            var options = recue.Options
                .Where(o => !string.IsNullOrWhiteSpace(o))
                .Select(o => o.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var existante in colonne.ColumnAllowedValues)
            {
                var index = options.FindIndex(o => string.Equals(o, existante.Value, StringComparison.OrdinalIgnoreCase));
                var active = index >= 0;
                var ordre = active ? index : existante.DisplayOrder;
                if (existante.Active != active || existante.DisplayOrder != ordre)
                {
                    await _context.ColumnAllowedValues
                        .Where(v => v.Id == existante.Id)
                        .ExecuteUpdateAsync(u => u.SetProperty(v => v.Active, active).SetProperty(v => v.DisplayOrder, ordre));
                }
            }

            nouvelles.AddRange(options
                .Select((valeur, ordre) => (valeur, ordre))
                .Where(o => !colonne.ColumnAllowedValues.Any(v => string.Equals(v.Value, o.valeur, StringComparison.OrdinalIgnoreCase)))
                .Select(o => new ColumnAllowedValue { ColumnDefinitionId = colonne.Id, Value = o.valeur, DisplayOrder = o.ordre, Active = true }));
        }

        if (nouvelles.Count > 0)
        {
            _context.ColumnAllowedValues.AddRange(nouvelles);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<System.Collections.Generic.List<RefFormulaireEquipe>> GetEquipesActivesByFormulaireIdAsync(Guid formulaireId)
    {
        return await _context.RefFormulaireEquipes
            .AsNoTracking()
            .Where(e => e.FormulaireId == formulaireId && e.Actif)
            .OrderBy(e => e.OrdreAffiche)
            .ToListAsync();
    }

    public async Task<List<Team>> GetEquipesReferenceAsync()
    {
        return await _context.Teams.AsNoTracking().Where(t => t.Active).OrderBy(t => t.Code).ToListAsync();
    }

    public async Task SyncEquipesAsync(Guid formulaireId, System.Collections.Generic.List<SopalTrace.Application.Helpers.EquipeJsonDto> parsedEquipes)
    {
        // AsNoTracking() is critical here: same reason as SyncColonnesAsync.
        var existingByName = (await _context.RefFormulaireEquipes
                .AsNoTracking()
                .Where(e => e.FormulaireId == formulaireId)
                .ToListAsync())
            .ToDictionary(e => e.NomEquipe, StringComparer.OrdinalIgnoreCase);

        // La Nième équipe configurée (dans l'ordre reçu) correspond au code d'équipe EN.
        var configurees = parsedEquipes.Where(p => !string.IsNullOrWhiteSpace(p.Nom)).ToList();

        // 1. Désactivation de toutes les équipes : les codes sont réattribués sans conflit
        //    avec l'unicité (une seule équipe active par code et par formulaire).
        await _context.RefFormulaireEquipes
            .Where(e => e.FormulaireId == formulaireId && e.Actif)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.Actif, false));

        var nouvelles = new System.Collections.Generic.List<RefFormulaireEquipe>();
        for (var position = 0; position < configurees.Count; position++)
        {
            var p = configurees[position];
            // Équipe choisie par le superviseur ; à défaut (ancien format), la Nième équipe est EN.
            var codeEquipe = string.IsNullOrWhiteSpace(p.Code) ? Equipes.PourPosition(position) : p.Code.Trim().ToUpperInvariant();

            if (existingByName.TryGetValue(p.Nom, out var existante))
            {
                // 2. Réactivation des équipes conservées, avec leur position et leurs horaires
                await _context.RefFormulaireEquipes
                    .Where(e => e.Id == existante.Id)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(e => e.TeamCode, codeEquipe)
                        .SetProperty(e => e.HeureDebut, p.Debut)
                        .SetProperty(e => e.HeureFin, p.Fin)
                        .SetProperty(e => e.OrdreAffiche, position)
                        .SetProperty(e => e.Actif, true));
            }
            else
            {
                nouvelles.Add(new RefFormulaireEquipe
                {
                    Id = Guid.NewGuid(),
                    FormulaireId = formulaireId,
                    TeamCode = codeEquipe,
                    NomEquipe = p.Nom,
                    HeureDebut = p.Debut,
                    HeureFin = p.Fin,
                    OrdreAffiche = position,
                    Actif = true
                });
            }
        }

        // 3. Ajout des nouvelles équipes
        if (nouvelles.Count > 0)
        {
            _context.RefFormulaireEquipes.AddRange(nouvelles);
            await _context.SaveChangesAsync();
        }
    }
}
