using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Execution.Operateur;

namespace SopalTrace.Application.Interfaces;

public interface IOccurrenceService
{
    /// <summary>
    /// Contrôles à faire, planifiés par le serveur (IPlanificationProductionService). Assemblage : ceux de
    /// l'équipe (par défaut celle qui a la main) sur le poste.
    /// </summary>
    Task<List<TrancheAlertesDto>> GetAlertesActivesAsync(Guid execControleOfId, string? posteCode = null, string? equipe = null);

    /// <summary>Alerte le superviseur (3 contrôles en retard) ou le manager (10), ou résout l'alerte.</summary>
    Task VerifierEscaladeAsync(Guid execControleOfId);
    Task GenererOccurrencesReglageCoursAsync(Guid execControleOfId);
    Task NettoyerOccurrencesReglageAsync(Guid execControleOfId);
    Task<bool> RepondreOccurrenceAsync(Guid occurrenceId, RepondreOccurrenceRequest request);
    Task<bool> IgnorerOccurrencesAsync(List<Guid> occurrenceIds, string matriculeOperateur, string? raison = null);
    Task<bool> DeclarerOccurrencesReglageAsync(List<Guid> occurrenceIds, string matriculeOperateur);
    Task AjouterRemarqueTrancheEnCoursAsync(Guid execControleOfId, string remarque);
}
