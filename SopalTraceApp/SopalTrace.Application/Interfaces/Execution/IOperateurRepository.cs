using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Interfaces.Execution;

public interface IOperateurRepository
{
    Task<MfgheadOrdreFabrication?> GetOfAsync(string numeroOf);
    Task<MagPreparationOf?> GetMagPreparationOfAsync(string numeroOf);
    Task<PlanFabricationEntete?> GetPlanActifAsync(string codeArticle);
    Task<ExecControleOf?> GetExecOfByIdAsync(Guid execOfId);
    Task<ExecControleOf?> GetExecOfWithIntermediairesAsync(Guid execOfId);
    Task<bool> HasReglageSectionsAsync(Guid planSourceId);
    void AddExecControleOf(ExecControleOf execOf);
    Task<IEnumerable<PosteTravail>> GetPostesDisponiblesAsync();
    Task<PosteTravail?> GetPosteWithMachinesAsync(string posteCode);
    Task<IEnumerable<SopalTrace.Application.DTOs.Execution.Operateur.OperateurOfDto>> GetAllOfOperationsDisponiblesAsync();
    Task<PlanFabricationLigne?> GetPlanLigneAsync(Guid ligneId);
    Task SaveChangesAsync();
}
