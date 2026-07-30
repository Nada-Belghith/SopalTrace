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
                .Where(p => p.NumeroOf == numeroOf)
                .ToListAsync();
        }

        public async Task<ExecRegistreTracabilite?> GetLatestLigneAsync(Guid execControleOfId)
        {
            return await _context.ExecRegistreTracabilites
                .Include(r => r.ExecRegistreTracabiliteComposants)
                .Where(r => r.ExecControleOfId == execControleOfId)
                .OrderByDescending(r => r.DateHeure)
                .FirstOrDefaultAsync();
        }

        public async Task AddLigneAsync(ExecRegistreTracabilite ligne)
        {
            _context.ExecRegistreTracabilites.Add(ligne);
            await _context.SaveChangesAsync();
        }

        public async Task<ExecRegistreTracabilite?> GetLigneAsync(Guid ligneId)
        {
            return await _context.ExecRegistreTracabilites
                .Include(r => r.ExecRegistreTracabiliteComposants)
                .FirstOrDefaultAsync(r => r.Id == ligneId);
        }

        public async Task DeleteLigneAsync(ExecRegistreTracabilite ligne)
        {
            _context.ExecRegistreTracabilites.Remove(ligne);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsLotValideAsync(string numeroLot, string natureArticleCode)
        {
            return await _context.ExecControleOfs
                .Include(e => e.NumeroOfNavigation)
                .ThenInclude(n => n.CodeArticleNavigation)
                .AnyAsync(e => e.NumeroOf == numeroLot && 
                               e.OperationCode == "USINAG" && 
                               (e.Statut == "EN_COURS" || e.Statut == "CLOTURE") &&
                               e.NumeroOfNavigation.CodeArticleNavigation.NatureArticleCode == natureArticleCode);
        }

        public async Task<List<string>> SearchLotsAsync(string natureArticleCode, string query)
        {
            var q = _context.ExecControleOfs
                .Include(e => e.NumeroOfNavigation)
                .ThenInclude(n => n.CodeArticleNavigation)
                .Where(e => e.OperationCode == "USINAG" && 
                            (e.Statut == "EN_COURS" || e.Statut == "CLOTURE") &&
                            e.NumeroOfNavigation.CodeArticleNavigation.NatureArticleCode == natureArticleCode);

            if (!string.IsNullOrWhiteSpace(query))
            {
                q = q.Where(e => e.NumeroOf.Contains(query));
            }

            return await q.Select(e => e.NumeroOf)
                          .Distinct()
                          .Take(20)
                          .ToListAsync();
        }

        public async Task<ExecControleDocumentStatut?> GetStatutTracabiliteAsync(Guid execControleOfId)
        {
            return await _context.ExecControleDocumentStatuts
                .FirstOrDefaultAsync(s => s.ExecControleOfId == execControleOfId && s.TypeDocument == "REGISTRE_TRACA");
        }

        public async Task AddStatutTracabiliteAsync(ExecControleDocumentStatut statut)
        {
            await _context.ExecControleDocumentStatuts.AddAsync(statut);
            await _context.SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
