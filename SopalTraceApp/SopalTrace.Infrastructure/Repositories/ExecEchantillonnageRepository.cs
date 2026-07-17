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

        public async Task<ExecEchantillonnage?> GetByExecControleOfIdAsync(Guid execControleOfId)
        {
            return await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                _context.Set<ExecEchantillonnage>(), x => x.ExecControleOfId == execControleOfId);
        }
    }
}
