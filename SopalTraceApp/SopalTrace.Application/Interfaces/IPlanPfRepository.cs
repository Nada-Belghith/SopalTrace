using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IPlanPfRepository
{
    Task<List<PlanProduitFiniEntete>> GetGenericPlansAsync();
    Task<PlanProduitFiniEntete?> GetPlanByIdAsync(Guid id);
    Task<PlanProduitFiniEntete?> GetPlanPourArchivageAsync(Guid id);
    
    Task<bool> ExistsActiveOrDraftPlanAsync(string familleProduitFiniCode);
    Task<PlanProduitFiniEntete?> GetDraftPlanByFamilleAsync(string familleProduitFiniCode);
    Task<PlanProduitFiniEntete?> GetPlanActifParFormulaireAsync(Guid formulaireId);
    
    Task AddPlanAsync(PlanProduitFiniEntete plan);
    Task<List<PlanProduitFiniEntete>> GetActivePlansByFamilleAsync(string familleProduitFiniCode);
    Task UpdatePlanAsync(PlanProduitFiniEntete plan);
    Task<int> GetDerniereVersionPlanAsync(string familleProduitFiniCode);
    
    Task SaveChangesAsync();
    void ClearTracking();
    void DeletePlan(PlanProduitFiniEntete plan);
}
