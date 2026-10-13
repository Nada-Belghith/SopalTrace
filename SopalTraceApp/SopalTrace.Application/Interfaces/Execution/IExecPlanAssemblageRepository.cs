using SopalTrace.Application.Services.Execution;
using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces.Execution
{
    public interface IExecPlanAssemblageRepository
    {
        Task<ExecControleOf?> GetExecOfAsync(Guid id);


        /// <summary>
        /// Plan d'assemblage actif applicable à l'article sur ce poste : plan de l'article ou de sa famille de
        /// produit fini ; celui rattaché au poste en priorité, sinon celui sans poste (valable sur tous les postes).
        /// Jamais le plan d'une autre famille.
        /// </summary>
        Task<Guid?> TrouverPlanAssemblageIdAsync(string codeArticle, string posteCode);
        Task<DocumentEntete?> GetDocumentByIdAsync(Guid docId);

        /// <summary>Famille de produit fini de l'article et son type de robinet (null si l'article n'est pas un PF).</summary>
        Task<(string FamilleCode, string TypeRobinetCode)?> GetFamilleArticleAsync(string codeArticle);

        /// <summary>Tous les statuts de documents de l'OF, toutes sessions (lecture seule).</summary>
        Task<List<ExecControleDocumentStatut>> GetStatutsOfAsync(Guid execControleOfId);

        Task<ExecControleDocumentStatut?> GetStatutPlanAssAsync(Guid execControleOfId, string posteCode, Guid sessionId);
        void AddStatut(ExecControleDocumentStatut statut);

        void AddLigneReponses(IEnumerable<ExecControleLigneReponse> reponses);

        /// <summary>Réponses saisies sur le plan d'assemblage du poste (première / dernière pièce, réglage, toutes les pièces), toutes sessions.</summary>
        Task<List<ReponseLot>> GetReponsesPlanAsync(Guid execControleOfId, string posteCode);

        /// <summary>Contrôles de production à faire (occurrences planifiées non répondues) de l'équipe sur le poste.</summary>
        Task<int> CompterOccurrencesAFaireAsync(Guid sessionId, string posteCode);

        /// <summary>Périodes de production de l'équipe sur le poste (lecture seule).</summary>
        Task<List<ExecProductionPeriod>> GetPeriodesProductionAsync(Guid sessionId, string posteCode);

        /// <summary>Effectif par poste de la fiche d'échantillonnage du poste (pièces à contrôler par heure).</summary>
        Task<int?> GetEffectifParPosteAsync(Guid execControleOfId, string posteCode);

        Task SaveChangesAsync();
    }
}
