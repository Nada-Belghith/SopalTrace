using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Interfaces.Execution
{
    public interface ITracabiliteRepository
    {
        Task<ExecControleOf?> GetExecOfAsync(Guid execControleOfId);
        /// <summary>Lignes du registre d'un poste de l'OF (chaque poste a son registre) ; tous les postes si aucun n'est donné.</summary>
        Task<List<ExecRegistreTracabilite>> GetHistoriqueAsync(Guid execControleOfId, string? posteCode);
        Task<List<MagPreparationOf>> GetPreparationsMagasinAsync(string numeroOf);
        Task<ExecRegistreTracabilite?> GetLigneAsync(Guid ligneId);
        void AddLigne(ExecRegistreTracabilite ligne);
        /// <summary>Confirmations des équipes (statuts du registre par session).</summary>
        Task<List<ExecControleDocumentStatut>> GetConfirmationsAsync(Guid execControleOfId);
        void AddStatut(ExecControleDocumentStatut statut);
        /// <summary>Formulaire en cours du registre de traçabilité (fiche de réponse FE0129), ou null.</summary>
        Task<Guid?> GetFormulaireRegistreAsync();

        /// <summary>
        /// Lot de corps / volant valide : numéro d'un OF d'usinage démarré dont l'article a cette nature ; si la
        /// nomenclature du produit fini contient des articles de cette nature, l'article doit en faire partie.
        /// </summary>
        /// <summary>Le lot est un OF d'usinage d'un article de cette nature présent dans la nomenclature du produit fini.</summary>
        Task<bool> IsLotValideAsync(string numeroLot, string natureArticleCode, string codeArticleProduitFini);
        /// <summary>La nomenclature du produit fini contient un article de cette nature (corps, volant).</summary>
        Task<bool> NomenclatureContientAsync(string natureArticleCode, string codeArticleProduitFini);
        Task<List<string>> SearchLotsAsync(string natureArticleCode, string codeArticleProduitFini, string query);
        Task SaveChangesAsync();
    }
}
