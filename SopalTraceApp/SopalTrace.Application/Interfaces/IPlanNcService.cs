using SopalTrace.Application.DTOs.QualityPlans.PlansNC;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IPlanNcService
{
    Task<Guid> CreerPlanAsync(CreatePlanNcRequestDto request, string creePar);
    Task<PlanNcResponseDto> GetPlanByIdAsync(Guid planId);
    Task<List<PlanNcResponseDto>> GetTousLesPlansAsync();
    Task<Guid> MettreAJourPlanAsync(Guid planId, SavePlanNcDto request, string modifiePar);
    Task<bool> MettreAJourLignesAsync(Guid planId, List<LigneNcEditDto> lignesModifiees);
    Task<Guid> CreerNouvelleVersionAsync(NouvelleVersionNcRequestDto request);
    Task<Guid> RestaurerPlanAsync(Guid planId, string restaurePar, string motif);
}
