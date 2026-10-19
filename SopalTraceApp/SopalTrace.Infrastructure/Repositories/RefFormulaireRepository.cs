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

    public async Task<ListesChampApplication> GetListesChampApplicationAsync() => new(
        await _context.FamilleProduitFinis.AsNoTracking().Where(f => f.Actif).OrderBy(f => f.Code)
            .Select(f => new ElementChampApplication(f.Code, f.Designation, f.TypeRobinetCode)).ToListAsync(),
        await _context.PosteTravails.AsNoTracking().Where(p => p.Actif).OrderBy(p => p.CodePoste)
            .Select(p => new ElementChampApplication(p.CodePoste, p.Libelle, null)).ToListAsync(),
        await _context.Machines.AsNoTracking().Where(m => m.Actif).OrderBy(m => m.CodeMachine)
            .Select(m => new ElementChampApplication(m.CodeMachine, m.Libelle, null)).ToListAsync(),
        await _context.NatureArticles.AsNoTracking().Where(n => n.Actif).OrderBy(n => n.Code)
            .Select(n => new ElementChampApplication(n.Code, n.Libelle, null)).ToListAsync(),
        await _context.TypeRobinets.AsNoTracking().Where(t => t.Actif).OrderBy(t => t.Code)
            .Select(t => new ElementChampApplication(t.Code, t.Libelle, null)).ToListAsync());

    public Task<List<RoleFormulaire>> GetRolesAsync() =>
        _context.FormRoles.AsNoTracking().Where(r => r.Active).OrderBy(r => r.DisplayOrder).ThenBy(r => r.Code)
            .Select(r => new RoleFormulaire(r.Code, r.Label, r.DisplayOrder, r.ScopeByFamily, r.ScopeByWorkstation, r.ScopeByMachine,
                r.ScopeByNature, r.ScopeByValveType, r.Nature))
            .ToListAsync();

    public Task<string?> GetTypeRobinetDeFamilleAsync(string familleCode) =>
        _context.FamilleProduitFinis.AsNoTracking().Where(f => f.Code == familleCode).Select(f => (string?)f.TypeRobinetCode)
            .FirstOrDefaultAsync();

    public Task<bool> CodeExisteAsync(string codeReference) =>
        _context.RefFormulaires.AnyAsync(f => f.CodeReference == codeReference);

    public Task<bool> CodeUtiliseParUnAutreFormulaireAsync(string codeReference, Guid groupe) =>
        _context.RefFormulaires.AnyAsync(f => f.CodeReference == codeReference && f.VersionGroupId != groupe);

    public Task<bool> CodeVersionExisteAsync(string codeReference, int version) =>
        _context.RefFormulaires.AnyAsync(f => f.CodeReference == codeReference && f.Version == version);

    public Task<bool> EstUtiliseAsync(Guid formulaireId) => RemplacementFormulaire.EstUtiliseAsync(_context, formulaireId);

    public Task DemanderModeleAsync(Guid formulaireId, string origine, string? motif) =>
        ModelesDesFormulaires.DemanderAsync(_context, formulaireId, origine, motif, null);

    public Task<bool> RecopierModeleAsync(Guid ancienId, Guid nouveauId, string motif) =>
        ModelesDesFormulaires.RecopierModeleAsync(_context, ancienId, nouveauId, null, motif);

    public Task DeplacerDemandesModeleAsync(Guid ancienId, Guid nouveauId) => ModelesDesFormulaires.DeplacerDemandesAsync(_context, ancienId, nouveauId);

    public Task AnnulerDemandesModeleAsync(Guid formulaireId) => ModelesDesFormulaires.AnnulerDemandesAsync(_context, formulaireId, null);

    public async Task ChangerCodeAsync(Guid formulaireId, string ancienCode, string nouveauCode)
    {
        var strategie = _context.Database.CreateExecutionStrategy();
        await strategie.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            // Formulaire jamais utilisé : son code n'est encore qu'un projet (la base refuse si un document l'utilise).
            await _context.RefFormulaires.Where(f => f.Id == formulaireId)
                .ExecuteUpdateAsync(s => s.SetProperty(f => f.CodeReference, nouveauCode));
            await _context.ReportTemplateInitialForms.Where(i => i.FormCodeReference == ancienCode)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.FormCodeReference, nouveauCode));
            await transaction.CommitAsync();
        });
    }

    public async Task<(Guid FormulaireId, int DocumentsRecrees)> RemplacerParNouveauCodeAsync(Guid formulaireId, string nouveauCode,
        string matricule)
    {
        var strategie = _context.Database.CreateExecutionStrategy();
        return await strategie.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            string? ancienCode = null;
            // Nouvelle ligne : même formulaire (lignée), même version, même champ d'application, nouveau code.
            var (nouveau, recrees) = await RemplacementFormulaire.RemplacerAsync(_context, formulaireId, ancien =>
            {
                ancienCode = ancien.CodeReference;
                return RemplacementFormulaire.Copie(ancien, nouveauCode, ancien.Version, matricule);
            }, matricule, ancien => $"Changement de code {ancien.CodeReference} → {nouveauCode}", recopierModele: true);
            _context.FormCodeChanges.Add(new FormCodeChange { PreviousFormId = formulaireId, NewFormId = nouveau.Id, ChangedBy = matricule });
            await _context.SaveChangesAsync();
            await _context.ReportTemplateInitialForms.Where(i => i.FormCodeReference == ancienCode)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.FormCodeReference, nouveauCode));
            await transaction.CommitAsync();
            return (nouveau.Id, recrees);
        });
    }

    public Task<List<DocumentActifFormulaire>> GetDocumentsActifsAsync(Guid formulaireId) =>
        RemplacementFormulaire.DocumentsGeneriquesEnCours(_context, formulaireId)
            .AsNoTracking()
            .OrderBy(d => d.Nom).ThenBy(d => d.Version)
            .Select(d => new DocumentActifFormulaire(d.Id, d.Nom, d.Version, d.Statut, d.TypeDocumentCodeNavigation.Libelle,
                d.Formulaire!.CodeReference, d.Formulaire.Version))
            .ToListAsync();

    public Task<RefFormulaire?> GetFormulaireEnCoursParChampAsync(SopalTrace.Application.Helpers.ChampFormulaire champ) =>
        _context.RefFormulaires
            .AsNoTracking()
            .Where(f => f.Role == champ.Role && (f.Statut == StatutsPlan.Actif || f.Statut == StatutsPlan.Brouillon)
                        && f.FamilleProduitFiniCode == champ.Famille && f.PosteCode == champ.Poste
                        && f.MachineCode == champ.Machine && f.NatureArticleCode == champ.Nature
                        // Avec une famille, le type de robinet est celui de la famille : il ne distingue rien de plus.
                        && (champ.Famille != null || f.TypeRobinetCode == champ.TypeRobinet))
            .OrderBy(f => f.Statut == StatutsPlan.Actif ? 0 : 1)
            .FirstOrDefaultAsync();

    /// <remarks>
    /// Un ancien code (gardé par les versions archivées) mène à la version en cours du même formulaire : les versions
    /// sont reliées par leur identifiant de groupe, pas par leur code.
    /// </remarks>
    public async Task<RefFormulaire?> GetFormulaireActifByCodeReferenceAsync(string codeReference)
    {
        var codeRefTrimmed = codeReference?.Trim();
        var groupes = _context.RefFormulaires.Where(f => f.CodeReference == codeRefTrimmed).Select(f => f.VersionGroupId);
        return await _context.RefFormulaires
            .AsNoTracking()
            .Where(f => groupes.Contains(f.VersionGroupId)
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

        // Une seule entrée par formulaire (groupe de versions) : priorité ACTIF, puis version la plus élevée
        return formulaires
            .GroupBy(f => f.VersionGroupId)
            .Select(g => g
                .OrderBy(f => f.Statut!.Trim() == StatutsPlan.Actif ? 0 : 1)
                .ThenByDescending(f => f.Version)
                .First())
            .OrderBy(f => f.Designation)
            .ToList();
    }

    public async Task<int> GetMaxVersionDuGroupeAsync(Guid groupe) =>
        await _context.RefFormulaires.Where(f => f.VersionGroupId == groupe).MaxAsync(f => (int?)f.Version) ?? 0;

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
