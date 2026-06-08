using SopalTrace.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IRefFormulaireRepository
{
    Task<RefFormulaire?> GetByIdAsync(Guid id);
    Task UpdateAsync(RefFormulaire entity);
    Task AddAsync(RefFormulaire entity);
}
