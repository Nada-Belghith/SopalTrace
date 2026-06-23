using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Interfaces.Repositories;

public interface IModeleFabricationEnteteRepository
{
    Task<ModeleFabricationEntete?> GetByIdAsync(Guid id, bool includeRelations = false);
    Task<IEnumerable<ModeleFabricationEntete>> GetAllAsync(bool includeRelations = false);
    Task AddAsync(ModeleFabricationEntete document);
    Task UpdateAsync(ModeleFabricationEntete document);
    Task DeleteAsync(ModeleFabricationEntete document);

    void RemoveSection(ModeleFabricationSection section);
    void RemoveLigne(ModeleFabricationLigne ligne);

    Task<IEnumerable<ModeleFabricationEntete>> GetByFormulaireIdAsync(Guid formulaireId);

    Task<ModeleFabricationEntete?> GetActifByReferenceAsync(string typeDocumentCode, string code, string? operationCode = null);
    
    Task<IEnumerable<ModeleFabricationEntete>> GetByFiltersAsync(
        string? natureComposantCode = null, 
        string? operationCode = null, 
        string? familleProduitCode = null,
        string? statut = null);

    Task<int> GetLatestVersionAsync(
        string code, 
        string? operationCode = null,
        string? natureComposantCode = null,
        string? familleProduitCode = null);
}
