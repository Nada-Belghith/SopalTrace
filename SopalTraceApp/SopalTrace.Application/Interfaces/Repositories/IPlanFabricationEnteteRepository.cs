using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Interfaces.Repositories;

public interface IPlanFabricationEnteteRepository
{
    Task<PlanFabricationEntete?> GetByIdAsync(Guid id, bool includeRelations = false);
    Task<IEnumerable<PlanFabricationEntete>> GetAllAsync(bool includeRelations = false);
    Task AddAsync(PlanFabricationEntete document);
    Task UpdateAsync(PlanFabricationEntete document);
    Task DeleteAsync(PlanFabricationEntete document);

    void RemoveSection(PlanFabricationSection section);
    void RemoveLigne(PlanFabricationLigne ligne);

    Task<IEnumerable<PlanFabricationEntete>> GetByFormulaireIdAsync(Guid formulaireId);

    Task<PlanFabricationEntete?> GetActifByReferenceAsync(string typeDocumentCode, string codeArticleSageVersionne, string? operationCode = null);
    
    Task<IEnumerable<PlanFabricationEntete>> GetByFiltersAsync(
        string? operationCode = null, 
        string? statut = null);

    Task<int> GetLatestVersionAsync(
        string codeArticleSageVersionne, 
        string? operationCode = null);
}
