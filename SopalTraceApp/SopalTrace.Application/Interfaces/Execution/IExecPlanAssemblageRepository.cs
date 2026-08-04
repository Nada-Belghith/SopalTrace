using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces.Execution
{
    public interface IExecPlanAssemblageRepository
    {
        Task<ExecControleOf?> GetExecOfAsync(Guid id);
        Task<DocumentEntete?> GetPlanAssemblageActifAsync(string codeArticle);
        Task<(DocumentEntete? PlanAss, DocumentEntete? DocRes)> GetDocumentsAssemblageEtResultatAsync(string codeArticle, string? posteCode = null);
        Task<int> GetPreviousExecOfsCountAsync(string numeroOf, DateTime? beforeDate = null);
        Task<int> GetPreviousEquipeExecCountAsync(string numeroOf, string equipe, DateTime? beforeDate = null);
        Task<DocumentEntete?> GetDocumentByIdAsync(Guid docId);
        Task<ExecEchantillonnage?> GetExecEchantillonnageAsync(Guid execControleOfId);
        Task<List<ExecControleTranche>> GetTranchesAsync(Guid execControleOfId);
        Task<ExecControleTranche?> GetTrancheByIdAsync(Guid trancheId);
        void AddTranche(ExecControleTranche tranche);
        void RemoveTranches(IEnumerable<ExecControleTranche> tranches);
        Task<ExecControleDocumentStatut?> GetStatutPlanAssAsync(Guid execControleOfId, string? posteCode = null);
        Task<ExecControleDocumentStatut?> GetStatutResultatCfAsync(Guid execControleOfId, Guid docId, string? posteCode = null);
        Task<List<ExecControleDocumentStatut>> GetStatutsAssemblageAsync(Guid execControleOfId);
        void AddStatut(ExecControleDocumentStatut statut);
        Task<Guid?> GetUtilisateurIdByMatriculeAsync(string? matricule);
        Task<Guid> GetDefaultUtilisateurIdAsync();
        void AddLigneReponses(IEnumerable<ExecControleLigneReponse> reponses);
        Task<List<ExecControleLigneReponse>> GetLigneReponsesAsync(Guid execControleOfId);
        Task<ExecControleLigneReponse?> GetLigneReponseByLigneIdAsync(Guid execControleOfId, Guid ligneId, string contexte);
        Task SaveChangesAsync();
    }
}
