using SopalTrace.Application.DTOs.QualityPlans.PlansEchantillonnage;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IPlanEchanService
{
    Task<Guid> CreerPlanAsync(CreatePlanEchanRequestDto request, string creePar);
    Task<PlanEchanResponseDto> GetPlanByIdAsync(Guid planId);
    Task<Guid> CreerNouvelleVersionAsync(NouvelleVersionEchanRequestDto request);
    Task<bool> MettreAJourPlanAsync(Guid planId, UpdatePlanEchanRequestDto request);
    Task<Guid> RestaurerPlanAsync(RestaurerEchanRequestDto request);

}