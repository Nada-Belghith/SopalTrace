using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces.Repositories;

public interface IDocumentEchantillonnageEnteteRepository
{
    Task<DocumentEchantillonnageEntete?> GetByIdAsync(Guid id, bool includeRelations = true);
    Task<DocumentEchantillonnageEntete?> GetPlanActifAsync();
    Task<IEnumerable<DocumentEchantillonnageEntete>> GetAllWithRelationsAsync();
    Task AddAsync(DocumentEchantillonnageEntete entity);
    Task UpdateAsync(DocumentEchantillonnageEntete entity);
    Task DeleteAsync(DocumentEchantillonnageEntete entity);
}
