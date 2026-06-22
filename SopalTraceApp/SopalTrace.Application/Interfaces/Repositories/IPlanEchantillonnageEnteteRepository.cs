using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces.Repositories;

public interface IPlanEchantillonnageEnteteRepository
{
    Task<PlanEchantillonnageEntete?> GetByIdAsync(Guid id, bool includeRelations = true);
    Task<PlanEchantillonnageEntete?> GetPlanActifAsync();
    Task<IEnumerable<PlanEchantillonnageEntete>> GetAllWithRelationsAsync();
    Task AddAsync(PlanEchantillonnageEntete entity);
    Task UpdateAsync(PlanEchantillonnageEntete entity);
    Task DeleteAsync(PlanEchantillonnageEntete entity);
}
