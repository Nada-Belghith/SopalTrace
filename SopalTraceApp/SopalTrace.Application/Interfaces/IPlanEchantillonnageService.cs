using SopalTrace.Application.DTOs.QualityPlans.PlansEchantillonnage;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IPlanEchantillonnageService
{
    Task<PlanEchanResponseDto?> GetPlanActifAsync();
    Task<PlanEchanResponseDto?> GetPlanByIdAsync(Guid id);
    Task<Guid> CreatePlanAsync(CreatePlanEchanRequestDto request, string creePar);
    Task UpdatePlanAsync(Guid id, UpdatePlanEchanRequestDto request);
    Task ActiverPlanAsync(Guid id, string modifiePar);
    Task<Guid> CreerNouvelleVersionAsync(NouvelleVersionEchanRequestDto request);
    Task<Guid> RestaurerPlanAsync(RestaurerEchanRequestDto request);
}
