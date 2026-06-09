using SopalTrace.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IRefFormulaireRepository
{
    Task<RefFormulaire?> GetByIdAsync(Guid id);
    Task UpdateAsync(RefFormulaire entity);
    Task AddAsync(RefFormulaire entity);
    Task<RefFormulaire?> GetFormulaireActifByRoleAsync(string role);
    Task<RefFormulaire?> GetFormulaireActifByCodeReferenceAsync(string codeReference);
    Task<System.Collections.Generic.IEnumerable<RefFormulaire>> GetFormulairesByRoleAsync(string role);
    Task<int> GetMaxVersionByCodeReferenceAsync(string codeReference);
}
