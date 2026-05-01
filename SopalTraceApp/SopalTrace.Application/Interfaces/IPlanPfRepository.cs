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
    Task<bool> ExistsActiveOrDraftPlanAsync(string typeRobinetCode);
    Task<PlanPfEntete?> GetDraftPlanByTypeRobinetAsync(string typeRobinetCode);
    Task AddPlanAsync(PlanPfEntete plan);
    Task<List<PlanPfEntete>> GetActivePlansByTypeRobinetAsync(string typeRobinetCode, Guid excludeId);
    Task<List<PlanPfEntete>> GetActivePlansByTypeRobinetAsync(string typeRobinetCode);
    Task UpdatePlanAsync(PlanPfEntete plan);
    Task<int> GetDerniereVersionPlanAsync(string typeRobinetCode);
    Task SaveChangesAsync();
    void ClearTracking();
    void DeletePlan(PlanPfEntete plan);
}
