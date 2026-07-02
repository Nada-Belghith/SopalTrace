using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Execution.Operateur;

namespace SopalTrace.Application.Interfaces;

public interface IOccurrenceService
{
    Task GenererOccurrencesInitialesAsync(Guid execControleOfId);
    Task<List<TrancheAlertesDto>> GetAlertesActivesAsync(Guid execControleOfId);
    Task ShiftOccurrencesApresPauseAsync(Guid execControleOfId);
    Task<bool> RepondreOccurrenceAsync(Guid occurrenceId, RepondreOccurrenceRequest request);
    Task<bool> IgnorerOccurrencesAsync(List<Guid> occurrenceIds, string matriculeOperateur);
}
