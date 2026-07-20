using System.Threading.Tasks;
using SopalTrace.Application.Interfaces.Repositories;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;

namespace SopalTrace.Infrastructure.Repositories
{
    public class ExecEchantillonnageRepository : IExecEchantillonnageRepository
    {
        private readonly SopalTraceDbContext _context;

        public ExecEchantillonnageRepository(SopalTraceDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(ExecEchantillonnage execEchantillonnage)
        {
            await _context.Set<ExecEchantillonnage>().AddAsync(execEchantillonnage);
        }

        public async Task<ExecEchantillonnage?> GetByExecControleOfIdAndPosteAsync(Guid execControleOfId, string? posteCode)
        {
            return await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.Include(_context.Set<ExecEchantillonnage>(), x => x.CodeInstruments), 
                x => x.ExecControleDocumentStatut.ExecControleOfId == execControleOfId 
                     && x.ExecControleDocumentStatut.PosteCode == posteCode
                     && x.ExecControleDocumentStatut.TypeDocument == "ECHANTILLONNAGE");
        }

        public async Task<ExecEchantillonnage?> GetByIdAsync(Guid id)
        {
            return await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.Include(
                    Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.Include(_context.Set<ExecEchantillonnage>(), x => x.CodeInstruments),
                    x => x.ExecControleDocumentStatut
                ),
                x => x.Id == id);
        }

        public Task UpdateAsync(ExecEchantillonnage execEchantillonnage)
        {
            _context.Set<ExecEchantillonnage>().Update(execEchantillonnage);
            return Task.CompletedTask;
        }
    }
}
