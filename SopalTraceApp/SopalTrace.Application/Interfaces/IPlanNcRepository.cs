using SopalTrace.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IPlanNcRepository
{
    Task<bool> ExistePlanActifAsync(string posteCode);
    Task<PlanNcEntete?> GetPlanActifAsync(string posteCode);
    Task<List<PlanNcEntete>> GetTousLesPlansAsync();
    Task<PlanNcEntete?> GetPlanAvecRelationsAsync(Guid planId);
    Task AddPlanAsync(PlanNcEntete plan);
    void AddLigne(PlanNcLigne ligne);
    void RemoveLigne(PlanNcLigne ligne);
    Task SaveChangesAsync();
}
