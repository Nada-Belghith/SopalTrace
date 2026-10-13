using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Application.Services.Execution;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Infrastructure.Repositories.Execution
{
    public class ExecPlanAssemblageRepository : IExecPlanAssemblageRepository
    {
        private readonly SopalTraceDbContext _context;

        public ExecPlanAssemblageRepository(SopalTraceDbContext context)
        {
            _context = context;
        }

        public async Task<ExecControleOf?> GetExecOfAsync(Guid id)
        {
            return await _context.ExecControleOfs
                .Include(e => e.NumeroOfNavigation)
                    .ThenInclude(o => o.CodeArticleNavigation)
                .Include(e => e.ExecControleOfPostes)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<ExecControleOfSession?> GetActiveSessionAsync(Guid execControleOfId)
        {
            return await _context.ExecControleOfSessions
                .AsNoTracking()
                .Where(s => s.ExecControleOfId == execControleOfId &&
                            (s.Statut == StatutsSession.EnCours || s.Statut == StatutsSession.EnPause))
                .OrderBy(s => s.Statut == StatutsSession.EnCours ? 0 : 1)
                .ThenByDescending(s => s.DateDebut)
                .FirstOrDefaultAsync();
        }

        public async Task<List<ExecControleOfSession>> GetSessionsOuvertesAsync(Guid execControleOfId)
        {
            return await _context.ExecControleOfSessions
                .AsNoTracking()
                .Where(s => s.ExecControleOfId == execControleOfId &&
                            (s.Statut == StatutsSession.EnCours || s.Statut == StatutsSession.EnPause))
                .OrderByDescending(s => s.DateDebut)
                .ToListAsync();
        }

        public async Task<Guid?> GetPremiereSessionIdAsync(Guid execControleOfId)
        {
            return await _context.ExecControleOfSessions
                .Where(s => s.ExecControleOfId == execControleOfId)
                .OrderBy(s => s.DateDebut)
                .Select(s => (Guid?)s.Id)
                .FirstOrDefaultAsync();
        }

        // ───────────────────────── Sélection du plan d'assemblage ─────────────────────────

        public async Task<Guid?> TrouverPlanAssemblageIdAsync(string codeArticle, string posteCode)
        {
            // Le plan dépend de la famille du produit fini (qui porte le type de robinet) : un article sans
            // famille n'a pas de plan d'assemblage.
            var famille = await _context.ProduitFinis
                .Where(p => p.CodeArticle == codeArticle)
                .Select(p => new { p.FamilleProduitFiniCode })
                .FirstOrDefaultAsync();
            if (famille == null) return null;

            var plans = (await _context.DocumentEntetes
                .AsNoTracking()
                .Where(p => p.Statut == DocumentStatuts.Actif && p.TypeDocumentCode == TypesDocument.PlanAssemblage &&
                            (p.CodeArticleSageVersionne == codeArticle || p.FamilleProduitFiniCode == famille.FamilleProduitFiniCode))
                .Select(p => new { p.Id, p.PosteCode, p.CodeArticleSageVersionne, p.CreeLe })
                .ToListAsync())
                .OrderByDescending(p => p.CreeLe)
                .ToList();

            bool DuPoste(string? poste) => string.Equals(poste, posteCode, StringComparison.OrdinalIgnoreCase);
            // Un plan sans poste (dépendant seulement de la famille ou de l'article) s'applique à tous les postes.
            bool Commun(string? poste) => string.IsNullOrEmpty(poste);
            bool DeLArticle(string? code) => string.Equals(code, codeArticle, StringComparison.OrdinalIgnoreCase);

            // Du plus précis au plus général : plan de l'article, puis de la famille ; poste exact, puis commun.
            return (plans.FirstOrDefault(p => DeLArticle(p.CodeArticleSageVersionne) && DuPoste(p.PosteCode))
                    ?? plans.FirstOrDefault(p => DeLArticle(p.CodeArticleSageVersionne) && Commun(p.PosteCode))
                    ?? plans.FirstOrDefault(p => !DeLArticle(p.CodeArticleSageVersionne) && DuPoste(p.PosteCode))
                    ?? plans.FirstOrDefault(p => !DeLArticle(p.CodeArticleSageVersionne) && Commun(p.PosteCode)))?.Id;
        }

        public async Task<DocumentEntete?> GetDocumentByIdAsync(Guid docId)
        {
            return await _context.DocumentEntetes
                .AsNoTracking()
                .AsSplitQuery()
                .Include(p => p.DocumentSections)
                    .ThenInclude(s => s.TypeSection)
                .Include(p => p.DocumentSections)
                    .ThenInclude(s => s.Periodicite)
                .Include(p => p.DocumentSections)
                    .ThenInclude(s => s.DocumentLignes)
                        .ThenInclude(l => l.Caracteristique)
                .Include(p => p.DocumentSections)
                    .ThenInclude(s => s.DocumentLignes)
                        .ThenInclude(l => l.TypeControle)
                .Include(p => p.DocumentSections)
                    .ThenInclude(s => s.DocumentLignes)
                        .ThenInclude(l => l.MoyenControle)
                .Include(p => p.DocumentSections)
                    .ThenInclude(s => s.DocumentLignes)
                        .ThenInclude(l => l.DocumentLigneExtraColonnes)
                .Include(p => p.DocumentSections)
                    .ThenInclude(s => s.DocumentLignes)
                        .ThenInclude(l => l.DocumentLineImage)
                .FirstOrDefaultAsync(p => p.Id == docId);
        }

        public async Task<(string FamilleCode, string TypeRobinetCode)?> GetFamilleArticleAsync(string codeArticle)
        {
            var famille = await _context.ProduitFinis
                .Where(p => p.CodeArticle == codeArticle)
                .Select(p => new { p.FamilleProduitFiniCode, p.FamilleProduitFiniCodeNavigation.TypeRobinetCode })
                .FirstOrDefaultAsync();
            return famille == null ? null : (famille.FamilleProduitFiniCode, famille.TypeRobinetCode);
        }

        public async Task<List<ExecControleDocumentStatut>> GetStatutsOfAsync(Guid execControleOfId)
        {
            return await _context.ExecControleDocumentStatuts
                .AsNoTracking()
                .Where(s => s.ExecControleOfId == execControleOfId)
                .ToListAsync();
        }

        // ───────────────────────── Statuts du plan d'assemblage ─────────────────────────

        public async Task<ExecControleDocumentStatut?> GetStatutPlanAssAsync(Guid execControleOfId, string posteCode, Guid sessionId)
        {
            return await _context.ExecControleDocumentStatuts
                .FirstOrDefaultAsync(s => s.ExecControleOfId == execControleOfId &&
                                          s.TypeDocument == TypesDocumentExecution.PlanAssemblage &&
                                          s.PosteCode == posteCode &&
                                          s.SessionId == sessionId);
        }

        public void AddStatut(ExecControleDocumentStatut statut)
        {
            _context.ExecControleDocumentStatuts.Add(statut);
        }

        // ───────────────────────── Réponses ─────────────────────────

        public void AddLigneReponses(IEnumerable<ExecControleLigneReponse> reponses)
        {
            _context.ExecControleLigneReponses.AddRange(reponses);
        }

        public async Task<List<ReponseLot>> GetReponsesPlanAsync(Guid execControleOfId, string posteCode)
        {
            return await _context.ExecControleLigneReponses
                .AsNoTracking()
                .Where(r => r.ExecControleOfid == execControleOfId &&
                            (r.Contexte == ContextesReponse.PremierePiece || r.Contexte == ContextesReponse.DernierePiece ||
                             r.Contexte == ContextesReponse.Reglage || r.Contexte == ContextesReponse.CentPourCent) &&
                            r.ExecControleDocumentStatut != null &&
                            r.ExecControleDocumentStatut.TypeDocument == TypesDocumentExecution.PlanAssemblage &&
                            r.ExecControleDocumentStatut.PosteCode == posteCode)
                .Select(r => new ReponseLot(r.DocumentLigneId, r.Contexte, r.ResultatType == ResultatsLigne.NonConforme,
                    r.DateSaisie, r.ExecControleDocumentStatut!.SessionId, r.Equipe, r.DetailsNc, r.ActionsCorrection))
                .ToListAsync();
        }

        public async Task<int> CompterOccurrencesAFaireAsync(Guid sessionId, string posteCode)
        {
            // Une occurrence n'est créée que lorsqu'elle est due : toutes celles non répondues sont à faire.
            return await _context.ExecPrelevementIntermediaires
                .Where(o => o.SessionId == sessionId && o.PosteCode == posteCode && !o.EstRepondu)
                .Select(o => o.HeureNotifPrevue)
                .Distinct()
                .CountAsync();
        }

        public async Task<List<ExecProductionPeriod>> GetPeriodesProductionAsync(Guid sessionId, string posteCode)
        {
            return await _context.ExecProductionPeriods
                .AsNoTracking()
                .Where(p => p.SessionId == sessionId && p.PosteCode == posteCode)
                .OrderBy(p => p.StartedAt)
                .ToListAsync();
        }

        public async Task<int?> GetEffectifParPosteAsync(Guid execControleOfId)
        {
            return await _context.ExecEchantillonnages
                .Where(e => e.ExecControleDocumentStatut.ExecControleOfId == execControleOfId)
                .OrderByDescending(e => e.ExecControleDocumentStatut.DateExecution)
                .Select(e => e.EffectifParPosteAb)
                .FirstOrDefaultAsync();
        }

        public async Task SaveChangesAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
            {
                throw ex.ToDomainExceptionOrSelf("Ce plan vient d'être initialisé depuis un autre poste. Rafraîchissez l'écran.");
            }
        }
    }
}
