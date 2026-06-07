using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Infrastructure.Repositories.Execution
{
    public class ExecEncfRepository : IExecEncfRepository
    {
        private readonly SopalTraceDbContext _context;

        public ExecEncfRepository(SopalTraceDbContext context)
        {
            _context = context;
        }

        public async Task<ExecControleOf> GetExecEncfAsync(Guid id)
        {
            return await _context.ExecControleOfs
                .Include(e => e.ExecPieceTypes)
                .Include(e => e.ExecControleTranches)
                .Include(e => e.NumeroOfNavigation)
                    .ThenInclude(of => of.CodeArticleNavigation)
                .Include(e => e.PosteCodeNavigation)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<ExecControleOf> GetEnCoursExecEncfByOfAsync(string numeroOf, string posteCode)
        {
            return await _context.ExecControleOfs
                .Include(e => e.ExecPieceTypes)
                .Include(e => e.ExecControleTranches)
                .Include(e => e.NumeroOfNavigation)
                    .ThenInclude(of => of.CodeArticleNavigation)
                .Include(e => e.PosteCodeNavigation)
                .Where(e => e.NumeroOf == numeroOf && e.PosteCode == posteCode && e.Statut == "EN_COURS")
                .OrderByDescending(e => e.DateDebut)
                .FirstOrDefaultAsync();
        }

        public async Task<MfgheadOrdreFabrication> GetOfDetailsAsync(string numeroOf)
        {
            return await _context.MfgheadOrdreFabrications
                .Include(o => o.CodeArticleNavigation)
                .FirstOrDefaultAsync(o => o.NumeroOf == numeroOf);
        }

        public async Task AddExecEncfAsync(ExecControleOf entity)
        {
            await _context.ExecControleOfs.AddAsync(entity);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public Task RemovePieceTypes(IEnumerable<ExecPieceType> items)
        {
            _context.ExecPieceTypes.RemoveRange(items);
            return Task.CompletedTask;
        }

        public Task RemoveTranches(IEnumerable<ExecControleTranche> items)
        {
            _context.ExecControleTranches.RemoveRange(items);
            return Task.CompletedTask;
        }

        public Task AddPieceType(ExecPieceType item)
        {
            _context.ExecPieceTypes.Add(item);
            return Task.CompletedTask;
        }

        public Task AddTranche(ExecControleTranche item)
        {
            _context.ExecControleTranches.Add(item);
            return Task.CompletedTask;
        }
    }
}
