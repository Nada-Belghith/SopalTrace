using SopalTrace.Application.DTOs.QualityPlans.Modeles;
using SopalTrace.Application.DTOs.QualityPlans.Plans;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IPlanFabricationService
{
    // Modèles
    Task<Guid> CreerModeleAsync(CreateModeleRequestDto request);
    Task<ModeleResponseDto> GetModeleByIdAsync(Guid modeleId);

    // Plans
    Task<Guid> InstancierPlanDepuisModeleAsync(CreatePlanRequestDto request);
    Task<PlanResponseDto> GetPlanByIdAsync(Guid planId);
    Task<bool> MettreAJourValeursPlanAsync(Guid planId, List<SectionEditDto> sectionsModifiees); // Utilise la Liberté Totale
    Task<bool> ChangerStatutPlanAsync(Guid planId, ChangePlanStatusRequestDto request, string modifiePar);

    // Avancé
    Task<Guid> ClonerPlanPourNouvelArticleAsync(ClonePlanRequestDto request);
    Task<Guid> CreerNouvelleVersionPlanAsync(NouvelleVersionRequestDto request);
}