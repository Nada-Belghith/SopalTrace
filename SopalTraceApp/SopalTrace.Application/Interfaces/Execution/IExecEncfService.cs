using System;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Execution;

namespace SopalTrace.Application.Interfaces.Execution
{
    /// <summary>Résultat contrôle en cours de fabrication (ENCF), document exécuté pendant un OF démarré.</summary>
    public interface IExecEncfService
    {
        /// <param name="execControleOfId">Exécution de l'OF.</param>
        Task<ExecEncfDto?> GetExecEncfAsync(Guid execControleOfId);

        /// <summary>
        /// Ouvre l'ENCF du poste sur l'exécution en cours de l'OF (créée au démarrage de l'OF, jamais ici).
        /// </summary>
        Task<ExecEncfDto> GetOrCreateExecEncfByOfAsync(string numeroOf, string posteCode, string matriculeOperateur);

        /// <summary>Enregistre les pièces types et résultats par tranche : ajouts et corrections, jamais de suppression.</summary>
        Task<ExecEncfDto?> SaveExecEncfAsync(ExecEncfDto dto, string matriculeOperateur);
    }
}
