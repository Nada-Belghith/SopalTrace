using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces.Repositories;

public interface IPlanVerifMachineEnteteRepository
{
    Task<PlanVerifMachineEntete?> GetByIdAsync(Guid id, bool includeRelations = true);
    Task<IEnumerable<PlanVerifMachineEntete>> GetAllWithRelationsAsync();
    Task<IEnumerable<PlanVerifMachineEntete>> GetByMachineCodeAsync(string machineCode);
    Task AddAsync(PlanVerifMachineEntete entity);
    Task UpdateAsync(PlanVerifMachineEntete entity);
    Task DeleteAsync(PlanVerifMachineEntete entity);
    Task<IEnumerable<PlanVerifMachineEntete>> GetByFormulaireIdAsync(Guid formulaireId);
}
