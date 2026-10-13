using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Tracabilite;

namespace SopalTrace.Application.Interfaces.Execution
{
    public interface ITracabiliteService
    {
        Task<RegistreTracabiliteDto> GetRegistreTracabiliteAsync(Guid execControleOfId);
        Task<RegistreTracabiliteRowDto> AddLigneTracabiliteAsync(AddRegistreTracabiliteDto dto, string matriculeOperateur);
        Task<bool> ValidateLotAsync(Guid execControleOfId, string typeArticle, string numeroLot);
        Task<List<string>> SearchLotsAsync(Guid execControleOfId, string typeArticle, string query);
        /// <summary>L'équipe en production confirme ses lots (avant sa fin de poste).</summary>
        Task ConfirmerEquipeAsync(Guid execControleOfId, string posteCode, string matriculeOperateur);
        /// <summary>Annule une ligne erronée (gardée dans l'historique avec son motif).</summary>
        Task AnnulerLigneAsync(Guid ligneId, string raison, string matriculeOperateur);
    }
}
