using System;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Execution.Operateur;

namespace SopalTrace.Application.Interfaces.Execution;

public interface IExecutionOfService
{
    Task<ExecControleOfDto> DemarrerOfAsync(DemarrerOfRequest request);
    Task<bool> MettreEnReglageAsync(Guid execControleOfId);
    Task<(bool Success, string Message)> ReprendreOfAsync(Guid execControleOfId);
    Task<bool> MettreEnPauseAsync(Guid execControleOfId, string raison);
    Task<bool> ReprendreDepuisPauseAsync(Guid execControleOfId);
    Task<bool> CloturerOfAsync(Guid execControleOfId);
    
    Task<bool> UpdatePlanLigneAsync(Guid ligneId, UpdatePlanLigneDto dto);
    Task<object> VerifierPlanActifAsync(string articleCode, string? operationCode = null);
    
    Task<bool> IgnorerTrancheAsync(Guid execControleOfId, string trancheHoraire, string matriculeOperateur, string? raison = null);
    Task<bool> DeclarerTrancheEnReglageAsync(Guid execControleOfId, string trancheHoraire, string matriculeOperateur);
}
