using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces.Repositories;

/// <summary>Plans d'échantillonnage : Document_Entete de type PLAN_ECHANTILLONNAGE avec ses paramètres (SamplingPlan).</summary>
public interface IDocumentEchantillonnageEnteteRepository
{
    Task<DocumentEntete?> GetByIdAsync(Guid id, bool includeRelations = true);
    Task<DocumentEntete?> GetPlanActifAsync();
    Task<IEnumerable<DocumentEntete>> GetAllWithRelationsAsync();
    Task AddAsync(DocumentEntete entity);
    Task UpdateAsync(DocumentEntete entity);
    Task DeleteAsync(DocumentEntete entity);
}
