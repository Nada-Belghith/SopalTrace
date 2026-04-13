using SopalTrace.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IPlanEchanRepository
{
    Task<bool> ExistePlanActifAsync(string codeReference);
    Task<PlanEchantillonnageEntete?> GetPlanActifAsync(string codeReference);
    Task<PlanEchantillonnageEntete?> GetPlanAvecRelationsAsync(Guid planId);

    Task AddPlanAsync(PlanEchantillonnageEntete plan);
    Task SaveChangesAsync();
}