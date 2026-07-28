using System;
using System.Threading.Tasks;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Interfaces.Execution
{
    public interface IExecRcPosteRepository
    {
        Task<ExecControleDocumentStatut?> GetStatutWithDetailsAsync(Guid execControleOfId, string posteCode, string equipe, DateTime dateExecution);
        Task<System.Collections.Generic.List<ExecControleDocumentStatut>> GetAllStatutsAsync(Guid execControleOfId, string posteCode);
        Task<ExecControleDocumentStatut?> GetStatutByIdWithDetailsAsync(Guid id);
        Task<DocumentEntete?> GetPlanByIdAsync(Guid id);
        Task<DocumentEntete?> GetPlanActifByPosteAsync(string posteCode, Guid execControleOfId);
        void RemoveHeures(System.Collections.Generic.IEnumerable<ExecRcPosteHeure> heures);
        void RemoveReponses(System.Collections.Generic.IEnumerable<ExecRcPosteReponse> reponses);
        void RemoveBilans(System.Collections.Generic.IEnumerable<ExecRcPosteBilan> bilans);
        void AddHeure(ExecRcPosteHeure heure);
        void AddReponse(ExecRcPosteReponse reponse);
        void AddBilan(ExecRcPosteBilan bilan);
        void AddStatut(ExecControleDocumentStatut statut);
        Task SaveChangesAsync();
    }
}
