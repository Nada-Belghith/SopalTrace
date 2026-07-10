using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Execution.Operateur;

namespace SopalTrace.Application.Interfaces;

public interface IOccurrenceService
{
    Task GenererOccurrencesInitialesAsync(Guid execControleOfId);
    Task<List<TrancheAlertesDto>> GetAlertesActivesAsync(Guid execControleOfId);
    Task GenererOccurrencesReglageCoursAsync(Guid execControleOfId);
    Task NettoyerOccurrencesReglageAsync(Guid execControleOfId);
    Task<bool> RepondreOccurrenceAsync(Guid occurrenceId, RepondreOccurrenceRequest request);
    Task<bool> IgnorerOccurrencesAsync(List<Guid> occurrenceIds, string matriculeOperateur, string? raison = null);
    Task<bool> DeclarerOccurrencesReglageAsync(List<Guid> occurrenceIds, string matriculeOperateur);
    Task AjouterRemarqueTrancheEnCoursAsync(Guid execControleOfId, string remarque);
}
