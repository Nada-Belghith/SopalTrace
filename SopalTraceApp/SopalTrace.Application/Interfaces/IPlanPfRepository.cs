using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IPlanPfRepository
{
    Task<List<PlanPfEntete>> GetGenericPlansAsync();
    Task<PlanPfEntete?> GetPlanByIdAsync(Guid id);
    Task<PlanPfEntete?> GetPlanPourArchivageAsync(Guid id);
    
    Task<bool> ExistsActiveOrDraftPlanAsync(string familleProduitFiniCode);
    Task<PlanPfEntete?> GetDraftPlanByFamilleAsync(string familleProduitFiniCode);
    
    Task AddPlanAsync(PlanPfEntete plan);
    Task<List<PlanPfEntete>> GetActivePlansByFamilleAsync(string familleProduitFiniCode);
    Task UpdatePlanAsync(PlanPfEntete plan);
    Task<int> GetDerniereVersionPlanAsync(string familleProduitFiniCode);
    
    Task SaveChangesAsync();
    void ClearTracking();
    void DeletePlan(PlanPfEntete plan);
}
