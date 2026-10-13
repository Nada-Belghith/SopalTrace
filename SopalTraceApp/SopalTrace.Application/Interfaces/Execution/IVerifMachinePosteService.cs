using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Execution.Operateur;

namespace SopalTrace.Application.Interfaces.Execution;

/// <summary>Vérifications machine d'un poste d'assemblage, par moment (démarrage, après la pause, fin de poste).</summary>
public interface IVerifMachinePosteService
{
    Task<VerifMachinesPosteDto> GetMachinesAsync(Guid execControleOfId, string posteCode, string periodicite);

    /// <summary>Ajoute une machine au poste pour l'équipe active (son document est créé) ; retourne l'identifiant du document.</summary>
    Task<Guid> AjouterMachineAsync(Guid execControleOfId, string posteCode, string machineCode, string matricule);

    /// <summary>Enregistre les réponses et valide le moment, uniquement si tous ses points sont répondus.</summary>
    Task ValiderAsync(Guid statutId, ValiderVerifMachineRequest request, string matricule);

    /// <summary>Rouvre un moment validé pour correction (session active uniquement).</summary>
    Task CorrigerAsync(Guid statutId, string periodicite, string matricule);

    Task<List<VerifMachineHistoriqueDto>> GetHistoriqueAsync(Guid execControleOfId, string posteCode, string? machineCode, string? equipe, DateTime? jour);
}
