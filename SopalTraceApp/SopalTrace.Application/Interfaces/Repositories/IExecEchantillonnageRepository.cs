using System;
using System.Threading.Tasks;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Interfaces.Repositories
{
    public interface IExecEchantillonnageRepository
    {
        Task AddAsync(ExecEchantillonnage execEchantillonnage);
        Task<ExecEchantillonnage?> GetByExecControleOfIdAndPosteAsync(Guid execControleOfId, string? posteCode);
        Task<ExecEchantillonnage?> GetByIdAsync(Guid id);
        Task UpdateAsync(ExecEchantillonnage execEchantillonnage);
    }
}
