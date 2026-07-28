using SopalTrace.Application.DTOs.Execution;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces.Execution
{
    public interface IExecPlanAssemblageService
    {
        Task<ExecPlanAssemblageDto> GetOrInitExecutionAsync(Guid execControleOfId, string? posteCode);
        Task<object> VerifierDocumentsAsync(Guid execControleOfId, string? posteCode);
        Task<ExecPlanAssemblageDto> ConfigurerHorairesAsync(Guid execControleOfId, ConfigHeuresPosteRequest request);
        Task<bool> SaveResultatAsync(Guid execControleOfId, SaveResultatAssRequest request);
        Task<bool> CloturerExecutionAsync(Guid execControleOfId);
    }
}
