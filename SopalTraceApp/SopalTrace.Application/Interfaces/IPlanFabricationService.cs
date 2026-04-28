using SopalTrace.Application.DTOs.QualityPlans.PlanFabrication;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IPlanFabricationService
{
    Task<Guid> InstancierPlanDepuisModeleAsync(CreatePlanRequestDto request);
    Task<PlanResponseDto> GetPlanByIdAsync(Guid planId);
    Task<bool> MettreAJourValeursPlanAsync(Guid planId, List<SectionEditDto> sectionsModifiees, string? legendeMoyens = null, string? remarques = null, bool finaliser = true, string? nom = null, string? modifiePar = null);
    Task<bool> ChangerStatutPlanAsync(Guid planId, ChangePlanStatusRequestDto request, string modifiePar);
    Task<Guid> ClonerPlanPourNouvelArticleAsync(ClonePlanRequestDto request);
    Task<Guid> CreerNouvelleVersionPlanAsync(NouvelleVersionRequestDto request);
    Task<Guid> RestaurerPlanArchiveAsync(RestaurerPlanRequestDto request);
    Task<bool> SupprimerBrouillonAsync(Guid planId);
    Task<IReadOnlyList<PlanResponseDto>> GetPlansByFiltersAsync(string? typeRobinetCode, string? natureComposantCode, string? operationCode);
}