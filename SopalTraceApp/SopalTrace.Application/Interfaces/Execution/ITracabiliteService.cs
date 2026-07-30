using System;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Tracabilite;

namespace SopalTrace.Application.Interfaces.Execution
{
    public interface ITracabiliteService
    {
        Task<RegistreTracabiliteDto> GetRegistreTracabiliteAsync(Guid execControleOfId);
        Task<RegistreTracabiliteRowDto> AddLigneTracabiliteAsync(AddRegistreTracabiliteDto dto);
        Task<bool> ValidateLotAsync(string typeArticle, string numeroLot);
        Task<List<string>> SearchLotsAsync(string typeArticle, string query);
        Task<bool> CloturerRegistreAsync(Guid execControleOfId, string posteCode, string matriculeOperateur);
        Task<bool> DeleteLigneTracabiliteAsync(Guid ligneId);
    }
}
