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

        public async Task<DocumentEntete?> GetDocumentActifAsync(string posteCode)
        {
            return await _context.DocumentEntetes
                .Where(d => d.TypeDocumentCode == TypesDocument.ResultatCF && d.Statut == DocumentStatuts.Actif
                            && (d.PosteCode == posteCode || d.PosteCode == null))
                .OrderByDescending(d => d.PosteCode == posteCode)   // le document du poste avant le document commun
                .ThenByDescending(d => d.Version)
                .FirstOrDefaultAsync();
        }

        public void AddStatut(ExecControleDocumentStatut statut) => _context.ExecControleDocumentStatuts.Add(statut);
        public void AddTranche(ExecControleTranche tranche) => _context.ExecControleTranches.Add(tranche);
        public void AddPieceType(ExecPieceType pieceType) => _context.ExecPieceTypes.Add(pieceType);
        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}
