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
        Task<ExecRegistreTracabilite?> GetLigneAsync(Guid ligneId);
        void AddLigne(ExecRegistreTracabilite ligne);
        /// <summary>Équipe en production sur l'OF (null si aucune).</summary>
        Task<ExecControleOfSession?> GetSessionEnCoursAsync(Guid execControleOfId);
        /// <summary>Confirmations des équipes (statuts du registre par session).</summary>
        Task<List<ExecControleDocumentStatut>> GetConfirmationsAsync(Guid execControleOfId);
        void AddStatut(ExecControleDocumentStatut statut);

        /// <summary>
        /// Lot de corps / volant valide : numéro d'un OF d'usinage démarré dont l'article a cette nature ; si la
        /// nomenclature du produit fini contient des articles de cette nature, l'article doit en faire partie.
        /// </summary>
        Task<bool> IsLotValideAsync(string numeroLot, string natureArticleCode, string codeArticleProduitFini);
        Task<List<string>> SearchLotsAsync(string natureArticleCode, string codeArticleProduitFini, string query);
        Task SaveChangesAsync();
    }
}
