using System;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Execution;

namespace SopalTrace.Application.Services
{
    public interface IExecEchantillonnageService
    {
        Task<ExecEchantillonnageDto> InitPlanPourOfAsync(Guid execControleOfId, string? posteCode = null, int? nbPostes = null);
        Task<ExecEchantillonnageDto?> GetPlanPourOfAsync(Guid execControleOfId, string? posteCode);
        Task<bool> SourcePlanExistsAsync(Guid execControleOfId);
    }
}
