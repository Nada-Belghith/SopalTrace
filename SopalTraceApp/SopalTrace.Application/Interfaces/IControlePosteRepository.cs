using SopalTrace.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IControlePosteRepository
{
    Task<bool> ExistePlanActifAsync(string posteCode);
    Task<bool> ExistePlanActifParFormulaireAsync(Guid formulaireId);
    Task<PlanControlePosteEntete?> GetPlanActifAsync(string posteCode);
    Task<PlanControlePosteEntete?> GetPlanActifParFormulaireAsync(Guid formulaireId);
    Task<List<PlanControlePosteEntete>> GetTousLesPlansAsync();
    Task<PlanControlePosteEntete?> GetPlanAvecRelationsAsync(Guid planId);
    Task AddPlanAsync(PlanControlePosteEntete plan);
    void AddLigne(PlanControlePosteLigne ligne);
    void RemoveLigne(PlanControlePosteLigne ligne);
    Task SaveChangesAsync();
}
