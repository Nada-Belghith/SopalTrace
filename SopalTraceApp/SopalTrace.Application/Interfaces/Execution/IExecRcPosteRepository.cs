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
        /// <summary>Saisies (réalisées, défauts, rebutées) des autres documents du poste pour l'OF (tous les jours).</summary>
        Task<List<ExecControleDocumentStatut>> GetAutresDocumentsPosteAsync(Guid execControleOfId, string posteCode, Guid exceptStatutId);
        /// <summary>OF et poste d'un document (lecture sans suivi, avant de prendre le verrou du poste).</summary>
        Task<(Guid ExecControleOfId, string PosteCode)?> GetOfEtPosteAsync(Guid statutId);
        /// <summary>
        /// Exécute l'opération dans une transaction, sous un verrou exclusif SQL Server propre à l'OF et au poste : les
        /// enregistrements du contrôle au poste d'un même poste passent l'un après l'autre (plusieurs tablettes,
        /// plusieurs serveurs). Le verrou est libéré à la fin de la transaction.
        /// </summary>
        Task<T> SousVerrouPosteAsync<T>(Guid execControleOfId, string posteCode, Func<Task<T>> operation);
        /// <summary>Quantité lancée de l'OF (ERP).</summary>
        Task<decimal> GetQuantiteLanceeAsync(Guid execControleOfId);
        Task<DocumentEntete?> GetPlanByIdAsync(Guid id);
        Task<DocumentEntete?> GetPlanActifByPosteAsync(string posteCode, Guid execControleOfId);
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
