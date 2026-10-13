using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Execution.Operateur;

namespace SopalTrace.Application.Interfaces.Execution;

/// <summary>Avancement d'une session d'assemblage (étape, tâches, fin de poste / fin d'OF contrôlée).</summary>
public interface IAvancementAssemblageService
{
    /// <summary>Avancement du poste pour l'équipe donnée (par défaut celle qui a la main sur l'OF).</summary>
    Task<AvancementAssemblageDto> GetAvancementAsync(Guid execControleOfId, string posteCode, string? equipe = null);

    /// <summary>Étape de chaque poste de l'OF pour l'équipe (utilisée pour planifier les contrôles de production).</summary>
    Task<Dictionary<string, string>> GetPhasesPostesAsync(Guid execControleOfId, string equipe);

    /// <summary>L'équipe demande la fin de poste ou la clôture de l'OF : les tâches de fin deviennent attendues.</summary>
    Task DemanderClotureAsync(Guid execControleOfId, string mode);

    Task AnnulerDemandeClotureAsync(Guid execControleOfId);

    /// <summary>Termine le poste ou clôture l'OF, uniquement si toutes les tâches de fin de tous les postes sont faites.</summary>
    Task TerminerAsync(Guid execControleOfId);

    /// <summary>
    /// Refuse (RegleMetierException) une fin de poste ou une clôture d'OF d'assemblage tant qu'une tâche de fin
    /// manque sur l'un des postes ; sans effet pour un OF qui n'est pas d'assemblage.
    /// </summary>
    Task VerifierFinPossibleAsync(Guid execControleOfId, string mode);
}
