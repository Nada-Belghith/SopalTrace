using System;
using System.Threading.Tasks;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Interfaces.Repositories
{
    public interface IExecEchantillonnageRepository
    {
        Task AddAsync(ExecEchantillonnage execEchantillonnage);
        Task<ExecEchantillonnage?> GetByExecControleOfIdAsync(Guid execControleOfId);
    }
}
