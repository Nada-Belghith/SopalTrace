using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Interfaces.Execution;

public interface IOperateurRepository
{
    Task<MfgheadOrdreFabrication?> GetOfAsync(string numeroOf);
    Task<MagPreparationOf?> GetMagPreparationOfAsync(string numeroOf);
    Task<PlanFabricationEntete?> GetPlanActifAsync(string codeArticle, string? operationCode = null);
    Task<ExecControleOf?> GetExecOfByIdAsync(Guid execOfId);
    Task<ExecControleOf?> GetExecOfWithIntermediairesAsync(Guid execOfId);
    Task<IEnumerable<string>> GetPostesForExecutionAsync(Guid execOfId);
    Task<bool> HasReglageSectionsAsync(Guid planSourceId);
    void AddExecControleOf(ExecControleOf execOf);
    Task<IEnumerable<PosteTravail>> GetPostesDisponiblesAsync();
    Task<PosteTravail?> GetPosteWithMachinesAsync(string posteCode);
    Task<IEnumerable<SopalTrace.Application.DTOs.Execution.Operateur.OperateurOfDto>> GetAllOfOperationsDisponiblesAsync();
    Task<PlanFabricationLigne?> GetPlanLigneAsync(Guid ligneId);
    Task<bool> AreAllOperationsClosedAsync(string numeroOf);
    Task<bool> CanStartOperationAsync(string numeroOf, string operationCode);
    Task SaveChangesAsync();
    
    // Nouveaux pour Assemblage
    Task<DocumentEntete?> GetPlanAssemblageActifAsync(string codeArticle);
    Task<IEnumerable<ExecControleDocumentStatut>> GetDocumentStatutsAsync(Guid execControleOfId);
    Task<ExecControleDocumentStatut?> GetDocumentStatutByIdAsync(Guid statutId);
    void AddExecControleDocumentStatut(ExecControleDocumentStatut statut);
    Task<IEnumerable<RefFormulaire>> GetFormulairesPourPosteAsync(string posteCode, string role);
    Task<IEnumerable<RefFormulaire>> GetFormulairesPourArticleAsync(string codeArticle, string role);
    Task<RefFormulaire?> GetFormulaireGlobalAsync(string role);
    Task<Dictionary<Guid, string>> GetFormulaireDesignationsAsync(IEnumerable<Guid> ids);
}
