using SopalTrace.Application.DTOs.QualityPlans.PlanAssemblage;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IPlanAssService
{
    Task<Guid> CreerPlanAsync(CreatePlanAssRequestDto request, string creePar);
    Task<PlanAssResponseDto> GetPlanByIdAsync(Guid planId);
    Task<bool> MettreAJourValeursPlanAsync(Guid planId, List<SectionAssEditDto> sectionsModifiees);
    Task<bool> ChangerStatutPlanAsync(Guid planId, ChangePlanAssStatusRequestDto request, string modifiePar);
    Task<Guid> ClonerExceptionDepuisMaitreAsync(CloneExceptionAssRequestDto request);
    Task<Guid> CreerNouvelleVersionPlanAsync(NouvelleVersionAssRequestDto request);
}