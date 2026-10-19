using SopalTrace.Application.DTOs.Execution;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces.Execution
{
    public interface IExecPlanAssemblageService
    {
        Task<ExecPlanAssemblageDto> GetOrInitExecutionAsync(Guid execControleOfId, string posteCode);
        Task SaveResultatAsync(Guid execControleOfId, SaveResultatAssRequest request, string matricule);
    }
}
