using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Interfaces.Execution
{
    /// <summary>
    /// Résultat contrôle au poste : comptages par ligne et par heure (Exec_ControleLigne_Reponse),
    /// pièces réalisées par tranche (Exec_RC_Poste_Reponse) et pièces rebutées (Exec_RC_Poste_Bilan).
    /// </summary>
    public interface IExecRcPosteRepository
    {
        Task<ExecControleDocumentStatut?> GetStatutWithDetailsAsync(Guid execControleOfId, string posteCode, string equipe, DateTime dateExecution);
        Task<List<ExecControleDocumentStatut>> GetAllStatutsAsync(Guid execControleOfId, string posteCode);
        Task<ExecControleDocumentStatut?> GetStatutByIdWithDetailsAsync(Guid id);
        Task<DocumentEntete?> GetPlanByIdAsync(Guid id);
        Task<DocumentEntete?> GetPlanActifByPosteAsync(string posteCode, Guid execControleOfId);
        /// <summary>Équipe en production sur l'OF (null si aucune).</summary>
        Task<ExecControleOfSession?> GetSessionEnCoursAsync(Guid execControleOfId);
        /// <summary>Validations des équipes (marqueurs par session) sur le poste : du jour de production donné, ou de tous les jours.</summary>
        Task<List<ExecControleDocumentStatut>> GetValidationsEquipesAsync(Guid execControleOfId, string posteCode, DateTime? jour = null);
        /// <summary>Équipes actives (et leurs heures) définies dans le formulaire du document.</summary>
        Task<List<RefFormulaireEquipe>> GetEquipesAsync(Guid? formulaireId);
        void AddComptage(ExecControleLigneReponse comptage);
        void RemoveComptage(ExecControleLigneReponse comptage);
        void AddReponse(ExecRcPosteReponse reponse);
        void RemoveReponse(ExecRcPosteReponse reponse);
        void AddBilan(ExecRcPosteBilan bilan);
        void AddStatut(ExecControleDocumentStatut statut);
        Task SaveChangesAsync();
    }
}
