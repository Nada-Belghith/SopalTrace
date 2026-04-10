using SopalTrace.Application.DTOs.QualityPlans.Modeles;
using SopalTrace.Application.DTOs.QualityPlans.PlanFabrication;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IPlanFabricationService
{
    Task<Guid> CreerModeleAsync(CreateModeleRequestDto request);
    Task<ModeleResponseDto> GetModeleByIdAsync(Guid modeleId);

    Task<Guid> InstancierPlanDepuisModeleAsync(CreatePlanRequestDto request);
    Task<PlanResponseDto> GetPlanByIdAsync(Guid planId);
    Task<bool> MettreAJourValeursPlanAsync(Guid planId, List<SectionEditDto> sectionsModifiees);
    Task<bool> ChangerStatutPlanAsync(Guid planId, ChangePlanStatusRequestDto request, string modifiePar);

    Task<Guid> ClonerPlanPourNouvelArticleAsync(ClonePlanRequestDto request);
    Task<Guid> CreerNouvelleVersionPlanAsync(NouvelleVersionRequestDto request);
}