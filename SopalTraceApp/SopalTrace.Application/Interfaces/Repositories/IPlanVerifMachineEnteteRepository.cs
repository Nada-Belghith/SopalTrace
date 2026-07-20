using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces.Repositories;

public interface IDocumentVerifMachineEnteteRepository
{
    Task<DocumentVerifMachineEntete?> GetByIdAsync(Guid id, bool includeRelations = true);
    Task<IEnumerable<DocumentVerifMachineEntete>> GetAllWithRelationsAsync();
    Task<IEnumerable<DocumentVerifMachineEntete>> GetByMachineCodeAsync(string machineCode);
    Task AddAsync(DocumentVerifMachineEntete entity);
    Task UpdateAsync(DocumentVerifMachineEntete entity);
    Task DeleteAsync(DocumentVerifMachineEntete entity);
    Task<IEnumerable<DocumentVerifMachineEntete>> GetByFormulaireIdAsync(Guid formulaireId);
}
