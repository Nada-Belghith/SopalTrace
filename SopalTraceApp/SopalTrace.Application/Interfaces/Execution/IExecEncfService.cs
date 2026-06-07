using SopalTrace.Application.DTOs.Execution;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces.Execution
{
    public interface IExecEncfService
    {
        Task<ExecEncfDto> GetExecEncfAsync(Guid id);
        
        /// <summary>
        /// Creates a new execution form for an OF or returns an existing one if it's already "EN_COURS".
        /// </summary>
        Task<ExecEncfDto> GetOrCreateExecEncfByOfAsync(string numeroOf, string posteCode);
        
        Task<ExecEncfDto> SaveExecEncfAsync(ExecEncfDto dto);
    }
}
