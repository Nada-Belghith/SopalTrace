using System;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Execution;

namespace SopalTrace.Application.Interfaces
{
    public interface IExecRcPosteService
    {
        Task<ExecRcPosteDto?> GetPlanPourOfAsync(Guid execControleOfId, string posteCode, string equipe);
        Task<ExecRcPosteDto?> GetPlanByStatutIdAsync(Guid statutId);
        /// <summary>Fiche vierge du document de contrôle au poste : ses lignes et ses équipes, sans aucune saisie.</summary>
        Task<ExecRcPosteDto?> GetFicheViergeAsync(Guid planId);
        Task<ExecRcPosteDto?> UpdatePlanAsync(Guid execControleDocumentStatutId, ExecRcPosteDto request, string matriculeOperateur);
        Task<bool> CloturerTousLesDocumentsAsync(Guid execControleOfId, string posteCode);
        /// <summary>L'équipe en production valide ses heures sur le document du jour.</summary>
        Task<ExecRcPosteDto?> ValiderEquipeAsync(Guid execControleDocumentStatutId, string matriculeOperateur);
        Task<ExecRcPosteDto?> CreerNouveauDocumentAsync(Guid execControleOfId, string posteCode, DateTime dateExecution, string equipe, string matriculeOperateur);
    }
}
