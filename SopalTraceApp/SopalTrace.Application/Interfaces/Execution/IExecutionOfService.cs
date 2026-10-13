using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Execution.Operateur;

namespace SopalTrace.Application.Interfaces.Execution;

public interface IExecutionOfService
{
    Task<ExecControleOfDto> DemarrerOfAsync(DemarrerOfRequest request);
    Task<bool> MettreEnReglageAsync(Guid execControleOfId);
    Task<(bool Success, string Message)> ReprendreOfAsync(Guid execControleOfId);

    /// <summary>
    /// Pause de l'OF de fabrication, ou de l'équipe d'assemblage (tous les postes qu'elle tient ; par défaut la
    /// seule équipe ouverte). L'OF d'assemblage n'est en pause que si toutes ses équipes le sont.
    /// </summary>
    Task<bool> MettreEnPauseAsync(Guid execControleOfId, string raison, string typePause, string? equipe = null);

    Task<bool> ReprendreDepuisPauseAsync(Guid execControleOfId, string? equipe = null);

    /// <summary>
    /// Termine des postes de la session (fin de poste) : une autre équipe peut les prendre. La session se termine
    /// avec son dernier poste ; l'OF reste en cours.
    /// </summary>
    Task<bool> TerminerPostesAsync(Guid execControleOfId, Guid sessionId, IReadOnlyCollection<string> posteCodes, string? matricule);

    Task<bool> CloturerOfAsync(Guid execControleOfId);

    Task<object> VerifierPlanActifAsync(string articleCode, string? operationCode = null);

    Task<bool> IgnorerTrancheAsync(Guid execControleOfId, string trancheHoraire, string matriculeOperateur, string? raison = null);
    Task<bool> DeclarerTrancheEnReglageAsync(Guid execControleOfId, string trancheHoraire, string matriculeOperateur);
}
