using SopalTrace.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IPlanNcRepository
{
    Task<bool> ExistePlanActifAsync(string posteCode);
    Task<bool> ExistePlanActifParFormulaireAsync(Guid formulaireId);
    Task<PlanNonConformiteEntete?> GetPlanActifAsync(string posteCode);
    Task<PlanNonConformiteEntete?> GetPlanActifParFormulaireAsync(Guid formulaireId);
    Task<List<PlanNonConformiteEntete>> GetTousLesPlansAsync();
    Task<PlanNonConformiteEntete?> GetPlanAvecRelationsAsync(Guid planId);
    Task AddPlanAsync(PlanNonConformiteEntete plan);
    void AddLigne(PlanNonConformiteLigne ligne);
    void RemoveLigne(PlanNonConformiteLigne ligne);
    Task SaveChangesAsync();
}
