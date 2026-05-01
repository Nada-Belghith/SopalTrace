using SopalTrace.Application.DTOs.QualityPlans.PlanProduitFini;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IPlanPfService
{
    Task<List<PlanPfEnteteDto>> GetGenericPlansAsync();
    Task<PlanPfEnteteDto?> GetPlanByIdAsync(Guid id);
    Task<Guid> CreateGenericPlanAsync(CreatePlanPfRequestDto dto, string creePar);
    Task<Guid> CreerNouvelleVersionAsync(NouvelleVersionPfRequestDto request);
    Task ArchiverPlanAsync(Guid id, string modifiePar);
    Task<Guid> RestaurerPlanArchiveAsync(RestaurerPfRequestDto request);
    Task<bool> SupprimerBrouillonAsync(Guid id);
}
