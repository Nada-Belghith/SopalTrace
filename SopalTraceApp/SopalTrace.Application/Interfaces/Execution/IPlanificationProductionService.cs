using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces.Execution;

/// <summary>
/// Planification des contrôles périodiques (assemblage et fabrication) par le serveur (voir
/// <see cref="Services.Execution.CalendrierProduction"/>) : appelée par la tâche de fond chaque minute et avant
/// chaque lecture de l'avancement ou des contrôles, pour que toutes les tablettes voient le même état.
/// </summary>
public interface IPlanificationProductionService
{
    /// <summary>OF à suivre (en production ou avec une période encore ouverte).</summary>
    Task<List<Guid>> GetOfsAPlanifierAsync();

    /// <summary>Ouvre / ferme les périodes de production et crée les contrôles devenus dus.</summary>
    Task SynchroniserAsync(Guid execControleOfId);

    /// <summary>Réglage commencé sur le poste : les contrôles de production s'arrêtent jusqu'à un réglage conforme.</summary>
    Task DemarrerReglageAsync(Guid execControleOfId, string posteCode);

    /// <summary>Réglage abandonné avant toute saisie : la production du poste reprend (nouveau calcul).</summary>
    Task AnnulerReglageAsync(Guid execControleOfId, string posteCode);
}
