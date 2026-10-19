using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces.Repositories;

public interface IDocumentVerifMachineEnteteRepository
{
    Task<DocumentEntete?> GetByIdAsync(Guid id, bool includeRelations = true);
    Task<IEnumerable<DocumentEntete>> GetAllWithRelationsAsync();
    Task<IEnumerable<DocumentEntete>> GetByMachineCodeAsync(string machineCode);
    Task AddAsync(DocumentEntete entity);
    Task UpdateAsync(DocumentEntete entity);
    Task DeleteAsync(DocumentEntete entity);
    Task<IEnumerable<DocumentEntete>> GetByFormulaireIdAsync(Guid formulaireId);
    void RemoveFamille(DocumentVerifMachineFamille famille);
    void AddFamille(DocumentVerifMachineFamille famille);
    void RemoveLigne(DocumentLigne ligne);
    void AddLigne(DocumentLigne ligne);
}
