using SopalTrace.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IPlanNcRepository
{
    Task<bool> ExistePlanActifAsync(string typeRobinetCode, string operationCode, string posteCode);
    Task<PlanNcEntete?> GetPlanActifAsync(string typeRobinetCode, string operationCode, string posteCode);
    Task<PlanNcEntete?> GetPlanAvecRelationsAsync(Guid planId);
    Task AddPlanAsync(PlanNcEntete plan);
    void AddColonne(PlanNcColonne colonne);
    void RemoveColonne(PlanNcColonne colonne);
    Task SaveChangesAsync();
}
