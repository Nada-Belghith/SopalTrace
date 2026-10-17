using SopalTrace.Domain.Constants;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;

namespace SopalTrace.Infrastructure.Repositories.Execution
{
    public class ExecEncfRepository : IExecEncfRepository
    {
        private readonly SopalTraceDbContext _context;

        public ExecEncfRepository(SopalTraceDbContext context)
        {
            _context = context;
        }

        private IQueryable<ExecControleOf> AvecDetails => _context.ExecControleOfs
            .AsSplitQuery()
            .Include(e => e.ExecPieceTypes)
            .Include(e => e.ExecControleTranches)
            .Include(e => e.ExecControleDocumentStatuts)
            .Include(e => e.ExecControleOfSessions)
            .Include(e => e.NumeroOfNavigation)
                .ThenInclude(of => of.CodeArticleNavigation)
            .Include(e => e.ExecControleOfPostes)
                .ThenInclude(p => p.PosteCodeNavigation);

        public async Task<ExecControleOf?> GetExecutionAsync(Guid execControleOfId)
        {
            return await AvecDetails.FirstOrDefaultAsync(e => e.Id == execControleOfId);
        }

        public async Task<ExecControleOf?> GetExecutionOuverteAsync(string numeroOf, string posteCode)
        {
            return await AvecDetails
                .Where(e => e.NumeroOf == numeroOf && e.Statut != StatutsExecution.Cloture
                            && (e.TypeOf != TypesOf.Assemblage || e.ExecControleOfPostes.Any(p => p.PosteCode == posteCode)))
                .OrderByDescending(e => e.DateDebut)
                .FirstOrDefaultAsync();
        }

        public async Task<Guid?> TrouverFormulaireAsync(string? codeArticle, string? posteCode)
        {
            // Famille (et son type de robinet) d'un produit fini ; un composant n'en a pas.
            var article = await _context.ProduitFinis.AsNoTracking()
                .Where(p => p.CodeArticle == codeArticle)
                .Select(p => new { p.FamilleProduitFiniCode, p.FamilleProduitFiniCodeNavigation!.TypeRobinetCode })
                .FirstOrDefaultAsync();
            return await FormulairesResultats.TrouverAsync(_context, RolesFormulaire.ResultatControleCf, posteCode,
                article?.FamilleProduitFiniCode, article?.TypeRobinetCode);
        }

        public async Task<Guid?> TrouverPlanAsync(ExecControleOf execution, string? posteCode)
        {
            if (execution.PlanSourceId is { } planFabrication) return planFabrication;
            var applique = await _context.ExecControleDocumentStatuts.AsNoTracking()
                .Where(s => s.ExecControleOfId == execution.Id && s.TypeDocument == TypesDocumentExecution.PlanAssemblage
                            && s.DocId != null && (posteCode == null || s.PosteCode == posteCode))
                .OrderBy(s => s.CreationOrder)
                .Select(s => s.DocId)
                .FirstOrDefaultAsync();
            if (applique != null || posteCode == null) return applique;
            // L'OF (et son article) est chargé avec l'exécution.
            var codeArticle = execution.NumeroOfNavigation?.CodeArticle;
            return codeArticle == null ? null : await new ExecPlanAssemblageRepository(_context).TrouverPlanAssemblageIdAsync(codeArticle, posteCode);
        }

        public void AddStatut(ExecControleDocumentStatut statut) => _context.ExecControleDocumentStatuts.Add(statut);
        public void AddTranche(ExecControleTranche tranche) => _context.ExecControleTranches.Add(tranche);
        public void AddPieceType(ExecPieceType pieceType) => _context.ExecPieceTypes.Add(pieceType);
        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}
