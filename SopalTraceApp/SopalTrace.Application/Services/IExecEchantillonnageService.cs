using System;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Execution;

namespace SopalTrace.Application.Services
{
    public interface IExecEchantillonnageService
    {
        Task<ExecEchantillonnageDto> InitPlanPourOfAsync(Guid execControleOfId, int nbPostes);
        Task<ExecEchantillonnageDto?> GetPlanPourOfAsync(Guid execControleOfId);
        Task<bool> SourcePlanExistsAsync(Guid execControleOfId);
    }
}
