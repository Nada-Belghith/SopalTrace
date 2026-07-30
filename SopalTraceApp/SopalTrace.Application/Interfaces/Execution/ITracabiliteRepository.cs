using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Interfaces.Execution
{
    public interface ITracabiliteRepository
    {
        Task<ExecControleOf?> GetExecOfAsync(Guid execControleOfId);
        Task<List<ExecRegistreTracabilite>> GetHistoriqueAsync(Guid execControleOfId);
        Task<List<MagPreparationOf>> GetPreparationsMagasinAsync(string numeroOf);
        Task<ExecRegistreTracabilite?> GetLatestLigneAsync(Guid execControleOfId);
        Task<ExecRegistreTracabilite?> GetLigneAsync(Guid ligneId);
        Task AddLigneAsync(ExecRegistreTracabilite ligne);
        Task DeleteLigneAsync(ExecRegistreTracabilite ligne);
        Task<bool> IsLotValideAsync(string numeroLot, string natureArticleCode);
        Task<List<string>> SearchLotsAsync(string natureArticleCode, string query);
        Task<ExecControleDocumentStatut?> GetStatutTracabiliteAsync(Guid execControleOfId);
        Task AddStatutTracabiliteAsync(ExecControleDocumentStatut statut);
        Task SaveChangesAsync();
    }
}
