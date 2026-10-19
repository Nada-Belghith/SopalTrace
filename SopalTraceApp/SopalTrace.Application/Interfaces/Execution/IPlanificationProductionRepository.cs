using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Interfaces.Execution;

/// <summary>Périodes de production et occurrences planifiées (assemblage et fabrication).</summary>
public interface IPlanificationProductionRepository
{
    /// <summary>OF à suivre : en production (équipe en production pour l'assemblage) ou avec une période encore ouverte.</summary>
    Task<List<Guid>> GetOfsAPlanifierAsync();

    /// <summary>Toutes les sessions d'équipe de l'OF (lecture seule).</summary>
    Task<List<ExecControleOfSession>> GetSessionsAsync(Guid execControleOfId);

    /// <summary>Toutes les périodes de production de l'OF (suivies pour mise à jour).</summary>
    Task<List<ExecProductionPeriod>> GetPeriodesAsync(Guid execControleOfId);

    /// <summary>Occurrences planifiées (rattachées à une période) non répondues de l'OF (suivies pour mise à jour).</summary>
    Task<List<ExecPrelevementIntermediaire>> GetOccurrencesNonReponduesAsync(Guid execControleOfId);

    /// <summary>Tranches horaires déjà soldées (résultat final : ignorée, déclarée en réglage…) de l'OF.</summary>
    Task<HashSet<string>> GetTranchesSoldeesAsync(Guid execControleOfId);

    void AjouterPeriode(ExecProductionPeriod periode);
    void AjouterOccurrences(IEnumerable<ExecPrelevementIntermediaire> occurrences);
    Task SaveChangesAsync();
}
