using System;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Execution;

namespace SopalTrace.Application.Services
{
    public interface IExecEchantillonnageService
    {
        Task<ExecEchantillonnageDto> InitPlanPourOfAsync(Guid execControleOfId, InitEchantillonnageRequest request);
        Task<ExecEchantillonnageDto?> GetPlanPourOfAsync(Guid execControleOfId, string? posteCode);
        Task<ExecEchantillonnageDto> UpdatePlanAsync(Guid id, ExecEchantillonnageDto request);
        Task<bool> SourcePlanExistsAsync(Guid execControleOfId);
    }
}
