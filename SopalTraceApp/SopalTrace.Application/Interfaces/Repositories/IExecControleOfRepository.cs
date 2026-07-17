using System;
using System.Threading.Tasks;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Interfaces.Repositories
{
    public interface IExecControleOfRepository
    {
        Task<ExecControleOf?> GetByIdWithOFAsync(Guid id);
    }
}
