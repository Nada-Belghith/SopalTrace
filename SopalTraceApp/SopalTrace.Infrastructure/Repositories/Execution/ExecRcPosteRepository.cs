using SopalTrace.Domain.Constants;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;
using SopalTrace.Infrastructure.Data;

namespace SopalTrace.Infrastructure.Repositories.Execution
{
    public class ExecRcPosteRepository : IExecRcPosteRepository
    {
        private readonly SopalTraceDbContext _context;

        public ExecRcPosteRepository(SopalTraceDbContext context)
        {
            _context = context;
        }

        public async Task<ExecControleDocumentStatut?> GetStatutWithDetailsAsync(Guid execControleOfId, string posteCode, string equipe, DateTime dateExecution)
        {
            if (string.IsNullOrEmpty(posteCode))
            {
                var execOf = await _context.ExecControleOfs.Include(e => e.ExecControleOfPostes).FirstOrDefaultAsync(e => e.Id == execControleOfId);
                posteCode = execOf?.ExecControleOfPostes.FirstOrDefault()?.PosteCode ?? "";
            }

            return await _context.ExecControleDocumentStatuts
                .Include(s => s.ExecControleLigneReponses)
                .Include(s => s.ExecRcPosteReponses)
                .Include(s => s.ExecRcPosteBilans)
                .FirstOrDefaultAsync(s => s.ExecControleOfId == execControleOfId 
                                          && s.PosteCode == posteCode 
                                          && s.TypeDocument == TypesDocumentExecution.ResultatControlePoste
                                          && s.DateExecution == dateExecution);
        }

        public async Task<List<ExecControleDocumentStatut>> GetAllStatutsAsync(Guid execControleOfId, string posteCode)
        {
            if (string.IsNullOrEmpty(posteCode))
            {
                var execOf = await _context.ExecControleOfs.Include(e => e.ExecControleOfPostes).FirstOrDefaultAsync(e => e.Id == execControleOfId);
                posteCode = execOf?.ExecControleOfPostes.FirstOrDefault()?.PosteCode ?? "";
            }

            return await _context.ExecControleDocumentStatuts
                .Where(s => s.ExecControleOfId == execControleOfId 
                            && s.PosteCode == posteCode 
                            && s.TypeDocument == TypesDocumentExecution.ResultatControlePoste)
                .OrderByDescending(s => s.DateExecution)
                .ToListAsync();
        }

        public async Task<List<ExecControleDocumentStatut>> GetAutresDocumentsPosteAsync(Guid execControleOfId, string posteCode, Guid exceptStatutId)
        {
            return await _context.ExecControleDocumentStatuts
                .AsNoTracking()
                .Include(s => s.ExecControleLigneReponses)
                .Include(s => s.ExecRcPosteReponses)
                .Include(s => s.ExecRcPosteBilans)
                .Where(s => s.ExecControleOfId == execControleOfId && s.PosteCode == posteCode && s.Id != exceptStatutId
                            && s.TypeDocument == TypesDocumentExecution.ResultatControlePoste)
                .ToListAsync();
        }

        public async Task<(Guid ExecControleOfId, string PosteCode)?> GetOfEtPosteAsync(Guid statutId)
        {
            var statut = await _context.ExecControleDocumentStatuts
                .AsNoTracking()
                .Where(s => s.Id == statutId)
                .Select(s => new { s.ExecControleOfId, s.PosteCode })
                .FirstOrDefaultAsync();
            return statut == null ? null : (statut.ExecControleOfId, statut.PosteCode ?? "");
        }

        public async Task<T> SousVerrouPosteAsync<T>(Guid execControleOfId, string posteCode, Func<Task<T>> operation)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                var resultat = new SqlParameter("@resultat", SqlDbType.Int) { Direction = ParameterDirection.Output };
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC @resultat = sp_getapplock @Resource = @ressource, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 15000",
                    resultat, new SqlParameter("@ressource", $"ControlePoste:{execControleOfId}:{posteCode}"));
                // 0 ou 1 : verrou obtenu ; négatif : délai dépassé ou interblocage.
                if ((int)resultat.Value < 0)
                    throw new ConflitConcurrenceException("Le contrôle au poste est en cours d'enregistrement sur une autre tablette : réessayez.");

                var valeur = await operation();
                await transaction.CommitAsync();
                return valeur;
            });
        }

        public async Task<decimal> GetQuantiteLanceeAsync(Guid execControleOfId)
        {
            return await _context.ExecControleOfs
                .Where(e => e.Id == execControleOfId)
                .Select(e => e.NumeroOfNavigation.QuantiteLancee)
                .FirstOrDefaultAsync();
        }

        public async Task<ExecControleDocumentStatut?> GetStatutByIdWithDetailsAsync(Guid id)
        {
            return await _context.ExecControleDocumentStatuts
                .Include(s => s.ExecControleLigneReponses)
                .Include(s => s.ExecRcPosteReponses)
                .Include(s => s.ExecRcPosteBilans)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<DocumentEntete?> GetPlanByIdAsync(Guid id)
        {
            return await _context.DocumentEntetes
                .Include(p => p.Formulaire)
                .Include(p => p.DocumentLignes)
                    .ThenInclude(l => l.RisqueDefaut)
                .FirstOrDefaultAsync(p => p.Id == id && p.TypeDocumentCode == TypesDocument.ControlePoste);
        }

        public async Task<DocumentEntete?> GetPlanActifByPosteAsync(string posteCode, Guid execControleOfId)
        {
            var execOf = await _context.ExecControleOfs.Include(e => e.ExecControleOfPostes).FirstOrDefaultAsync(e => e.Id == execControleOfId);
            if (string.IsNullOrEmpty(posteCode) && execOf != null)
            {
                posteCode = execOf.ExecControleOfPostes.FirstOrDefault()?.PosteCode ?? "";
            }

            // L'OF garde le document de contrôle au poste avec lequel il a commencé sur ce poste, même archivé depuis.
            if (await DocumentsDeLOf.DocumentDejaUtiliseAsync(_context, execControleOfId, TypesDocumentExecution.ResultatControlePoste, posteCode)
                is Guid dejaUtilise && await GetPlanByIdAsync(dejaUtilise) is { } planDeLOf)
                return planDeLOf;

            var of = execOf != null ? await _context.MfgheadOrdreFabrications.FirstOrDefaultAsync(o => o.NumeroOf == execOf.NumeroOf) : null;
            var familleCode = of != null
                ? await _context.ProduitFinis.Where(p => p.CodeArticle == of.CodeArticle).Select(p => p.FamilleProduitFiniCode).FirstOrDefaultAsync()
                : null;
            

            var plans = await _context.DocumentEntetes
                .Include(p => p.Formulaire)
                .Include(p => p.DocumentLignes)
                    .ThenInclude(l => l.RisqueDefaut)
                .Where(p => (string.IsNullOrEmpty(posteCode) || p.PosteCode == posteCode || p.PosteCode == null) &&
                            p.TypeDocumentCode == TypesDocument.ControlePoste && 
                            p.Statut == StatutsPlan.Actif)
                .ToListAsync();
                
            // Une famille qui a son propre contrôle au poste (ex. avec soupape : formulaire « PAS71 soupape ») n'utilise
            // que les documents de sa famille, même tant qu'aucun n'est encore créé : le document général du poste ne
            // la remplace jamais. Elle le sait par le champ d'application enregistré en base (poste et famille du
            // formulaire, ou d'un document déjà créé), jamais par le code ou la désignation. Les autres familles
            // utilisent le document général du poste (sans famille). À égalité, celui du poste passe avant celui sans
            // poste. Sans document, pas de contrôle : la tâche est signalée « document manquant ».
            var familleDediee = !string.IsNullOrEmpty(familleCode) && (
                await _context.RefFormulaires.AnyAsync(f =>
                    f.Role == RolesFormulaire.ResultatControlePoste && f.Statut != StatutsPlan.Archive &&
                    f.FamilleProduitFiniCode == familleCode && (f.PosteCode == null || f.PosteCode == posteCode))
                || await _context.DocumentEntetes.AnyAsync(d =>
                    d.TypeDocumentCode == TypesDocument.ControlePoste && d.Statut == StatutsPlan.Actif &&
                    d.FamilleProduitFiniCode == familleCode && (d.PosteCode == null || d.PosteCode == posteCode)));

            return plans
                .Where(p => familleDediee
                    ? string.Equals(p.FamilleProduitFiniCode, familleCode, StringComparison.OrdinalIgnoreCase)
                    : string.IsNullOrEmpty(p.FamilleProduitFiniCode))
                .OrderBy(p => p.PosteCode == null ? 1 : 0)
                .FirstOrDefault();
        }

        public async Task<List<ExecControleDocumentStatut>> GetValidationsEquipesAsync(Guid execControleOfId, string posteCode, DateTime? jour = null)
        {
            // Le marqueur porte le jour de production du document validé (DateExecution) et l'heure de validation (DateTermine).
            return await _context.ExecControleDocumentStatuts
                .AsNoTracking()
                .Where(s => s.ExecControleOfId == execControleOfId && s.PosteCode == posteCode &&
                            s.TypeDocument == TypesDocumentExecution.ClotureResultatControlePoste &&
                            s.SessionId != null && (jour == null || s.DateExecution == jour.Value.Date))
                .OrderBy(s => s.DateTermine)
                .ToListAsync();
        }

        public async Task<List<RefFormulaireEquipe>> GetEquipesAsync(Guid? formulaireId)
        {
            if (!formulaireId.HasValue) return new List<RefFormulaireEquipe>();
            return await _context.RefFormulaireEquipes
                .AsNoTracking()
                .Where(e => e.FormulaireId == formulaireId.Value && e.Actif)
                .OrderBy(e => e.OrdreAffiche)
                .ToListAsync();
        }

        public void AddComptage(ExecControleLigneReponse comptage) => _context.ExecControleLigneReponses.Add(comptage);
        public void RemoveComptage(ExecControleLigneReponse comptage) => _context.ExecControleLigneReponses.Remove(comptage);
        public void AddReponse(ExecRcPosteReponse reponse) => _context.ExecRcPosteReponses.Add(reponse);
        public void RemoveReponse(ExecRcPosteReponse reponse) => _context.ExecRcPosteReponses.Remove(reponse);
        public void AddBilan(ExecRcPosteBilan bilan) => _context.ExecRcPosteBilans.Add(bilan);
        public void AddStatut(ExecControleDocumentStatut statut) => _context.ExecControleDocumentStatuts.Add(statut);

        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}
