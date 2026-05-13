using SopalTrace.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IPlanEchanRepository
{
    Task<bool> ExistePlanActifAsync();
    Task<PlanEchantillonnageEntete?> GetPlanActifAsync();
    Task<PlanEchantillonnageEntete?> GetPlanAvecRelationsAsync(Guid planId);

    Task AddPlanAsync(PlanEchantillonnageEntete plan);
    Task<int> GetOrCreateNqaAsync(double valeur);
    Task SaveChangesAsync();
}