using SopalTrace.Application.DTOs.QualityPlans.PlansNC;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IPlanNcService
{
    Task<Guid> CreerPlanAsync(CreatePlanNcRequestDto request, string creePar);
    Task<PlanNcResponseDto> GetPlanByIdAsync(Guid planId);
    Task<bool> MettreAJourColonnesAsync(Guid planId, List<ColonneNcEditDto> colonnesModifiees);
    Task<Guid> CreerNouvelleVersionAsync(NouvelleVersionNcRequestDto request);
}
