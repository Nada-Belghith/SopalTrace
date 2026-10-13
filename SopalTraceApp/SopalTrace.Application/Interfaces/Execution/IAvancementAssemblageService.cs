using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Execution.Operateur;

namespace SopalTrace.Application.Interfaces.Execution;

/// <summary>
/// Avancement d'un OF d'assemblage poste par poste : chaque poste suit son propre parcours (étape, tâches, fin de
/// poste contrôlée), tenu par une seule équipe à la fois ; la clôture de l'OF couvre tous les postes.
/// </summary>
public interface IAvancementAssemblageService
{
    /// <summary>
    /// Avancement du poste pour l'équipe donnée (par défaut celle qui tient le poste, ou celle qui clôture l'OF).
    /// </summary>
    Task<AvancementAssemblageDto> GetAvancementAsync(Guid execControleOfId, string posteCode, string? equipe = null);

    /// <summary>L'équipe prend un poste libre de l'OF (ses contrôles de démarrage puis de fin deviennent dus).</summary>
    Task DemarrerPosteAsync(Guid execControleOfId, string posteCode, string? equipe, string matricule);

    /// <summary>Étape de chaque poste de l'OF pour l'équipe (utilisée pour planifier les contrôles de production).</summary>
    Task<Dictionary<string, string>> GetPhasesPostesAsync(Guid execControleOfId, string equipe);

    /// <summary>
    /// Fin de poste sur un poste (ses tâches de fin deviennent attendues, les autres postes continuent) ou clôture
    /// de l'OF (tous les postes ; l'équipe doit être seule sur l'OF).
    /// </summary>
    Task DemanderClotureAsync(Guid execControleOfId, string mode, string posteCode, string? equipe = null);

    /// <summary>Annule la clôture de l'OF demandée par l'équipe, sinon la fin de poste demandée sur le poste.</summary>
    Task AnnulerDemandeClotureAsync(Guid execControleOfId, string posteCode, string? equipe = null);

    /// <summary>
    /// Termine le poste (ses tâches de fin faites) ou clôture l'OF (tâches de fin de tous les postes faites). La
    /// session de l'équipe se termine avec son dernier poste.
    /// </summary>
    Task TerminerAsync(Guid execControleOfId, string posteCode, string? equipe, string matricule);

    /// <summary>
    /// Fin de poste directe d'une équipe (sans demande préalable) : le poste donné, sinon tous ceux qu'elle tient.
    /// <paramref name="verifier"/> faux : fin forcée (administrateur), sans contrôle des tâches de fin.
    /// </summary>
    Task TerminerPostesDirectementAsync(Guid execControleOfId, string? equipe, string? posteCode, string matricule, bool verifier);

    /// <summary>
    /// Refuse (RegleMetierException) la fin d'un poste (ou de tous les postes de l'équipe si aucun n'est donné) ou
    /// la clôture de l'OF tant qu'une tâche de fin manque ; sans effet pour un OF qui n'est pas d'assemblage.
    /// </summary>
    Task VerifierFinPossibleAsync(Guid execControleOfId, string mode, string? posteCode = null, string? equipe = null);
}
