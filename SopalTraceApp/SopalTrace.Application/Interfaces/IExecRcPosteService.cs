using System;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Execution;

namespace SopalTrace.Application.Interfaces
{
    public interface IExecRcPosteService
    {
        Task<ExecRcPosteDto> GetPlanPourOfAsync(Guid execControleOfId, string posteCode, string equipe);
        Task<ExecRcPosteDto> GetPlanByStatutIdAsync(Guid statutId);
        Task<ExecRcPosteDto> UpdatePlanAsync(Guid execControleDocumentStatutId, ExecRcPosteDto request, string matriculeOperateur);
        Task<bool> CloturerTousLesDocumentsAsync(Guid execControleOfId, string posteCode);
        Task<ExecRcPosteDto> CreerNouveauDocumentAsync(Guid execControleOfId, string posteCode, DateTime dateExecution, string equipe, string matriculeOperateur);
    }
}
