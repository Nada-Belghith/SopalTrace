using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces.Repositories;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;

namespace SopalTrace.Infrastructure.Repositories
{
    public class ExecControleOfRepository : IExecControleOfRepository
    {
        private readonly SopalTraceDbContext _context;

        public ExecControleOfRepository(SopalTraceDbContext context)
        {
            _context = context;
        }

        public async Task<ExecControleOf?> GetByIdWithOFAsync(Guid id)
        {
            return await _context.ExecControleOfs
                .Include(x => x.NumeroOfNavigation)
                    .ThenInclude(n => n.CodeArticleNavigation)
                .FirstOrDefaultAsync(x => x.Id == id);
        }
    }
}
