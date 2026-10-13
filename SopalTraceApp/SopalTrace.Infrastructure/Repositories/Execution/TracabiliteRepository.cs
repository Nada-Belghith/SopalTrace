using SopalTrace.Domain.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;

namespace SopalTrace.Infrastructure.Repositories.Execution
{
    public class TracabiliteRepository : ITracabiliteRepository
    {
        private readonly SopalTraceDbContext _context;

        public TracabiliteRepository(SopalTraceDbContext context)
        {
            _context = context;
        }

        public async Task<ExecControleOf?> GetExecOfAsync(Guid execControleOfId)
        {
            return await _context.ExecControleOfs
                .Include(e => e.NumeroOfNavigation)
                .Include(e => e.ExecControleOfPostes)
                .FirstOrDefaultAsync(e => e.Id == execControleOfId);
        }

        public async Task<List<ExecRegistreTracabilite>> GetHistoriqueAsync(Guid execControleOfId)
        {
            return await _context.ExecRegistreTracabilites
                .Include(r => r.ExecRegistreTracabiliteComposants)
                .Where(r => r.ExecControleOfId == execControleOfId)
                .OrderBy(r => r.DateHeure)
                .ToListAsync();
        }

        public async Task<List<MagPreparationOf>> GetPreparationsMagasinAsync(string numeroOf)
        {
            return await _context.MagPreparationOfs
                .Include(p => p.MagPreparationOfLots)
                .ThenInclude(l => l.CodeArticleNavigation)
                .Where(p => p.NumeroOf == numeroOf && p.Statut != StatutsPreparationMagasin.Annule)
                .ToListAsync();
        }

        public async Task<ExecRegistreTracabilite?> GetLigneAsync(Guid ligneId)
        {
            return await _context.ExecRegistreTracabilites
                .Include(r => r.ExecRegistreTracabiliteComposants)
                .FirstOrDefaultAsync(r => r.Id == ligneId);
        }

        public void AddLigne(ExecRegistreTracabilite ligne) => _context.ExecRegistreTracabilites.Add(ligne);

        public async Task<ExecControleOfSession?> GetSessionEnCoursAsync(Guid execControleOfId)
        {
            return await _context.ExecControleOfSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ExecControleOfId == execControleOfId && s.Statut == StatutsSession.EnCours);
        }

        public async Task<List<ExecControleDocumentStatut>> GetConfirmationsAsync(Guid execControleOfId)
        {
            return await _context.ExecControleDocumentStatuts
                .AsNoTracking()
                .Where(s => s.ExecControleOfId == execControleOfId && s.TypeDocument == TypesDocumentExecution.RegistreTracabilite && s.SessionId != null)
                .OrderBy(s => s.DateTermine)
                .ToListAsync();
        }

        public void AddStatut(ExecControleDocumentStatut statut) => _context.ExecControleDocumentStatuts.Add(statut);

        private IQueryable<ExecControleOf> LotsPossibles(string natureArticleCode, string codeArticleProduitFini)
        {
            // Composants de cette nature dans la nomenclature du produit fini (vide si elle ne les connaît pas).
            var composantsNomenclature = _context.BomdNomenclatures
                .Where(b => b.ArticleParent == codeArticleProduitFini && b.CodeComposantNavigation.NatureArticleCode == natureArticleCode)
                .Select(b => b.CodeComposant);

            return _context.ExecControleOfs
                .Where(e => e.OperationCode == CodesOperation.Usinage &&
                            (e.Statut == StatutsExecution.EnCours || e.Statut == StatutsExecution.Cloture) &&
                            e.NumeroOfNavigation.CodeArticleNavigation.NatureArticleCode == natureArticleCode &&
                            (!composantsNomenclature.Any() || composantsNomenclature.Contains(e.NumeroOfNavigation.CodeArticle)));
        }

        public async Task<bool> IsLotValideAsync(string numeroLot, string natureArticleCode, string codeArticleProduitFini)
        {
            return await LotsPossibles(natureArticleCode, codeArticleProduitFini).AnyAsync(e => e.NumeroOf == numeroLot);
        }

        public async Task<List<string>> SearchLotsAsync(string natureArticleCode, string codeArticleProduitFini, string query)
        {
            var q = LotsPossibles(natureArticleCode, codeArticleProduitFini);
            if (!string.IsNullOrWhiteSpace(query))
                q = q.Where(e => e.NumeroOf.Contains(query));
            return await q.Select(e => e.NumeroOf).Distinct().Take(20).ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
