using System;
using System.Threading.Tasks;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Interfaces.Execution
{
    /// <summary>
    /// Résultat contrôle en cours de fabrication (ENCF) : fiche de réponse remplie pendant un OF démarré, sur la même
    /// exécution que les autres documents (pièces types et résultats par tranche de l'OF). Son formulaire se trouve par
    /// famille (donc type de robinet) et poste ; ses lignes viennent du plan suivi par l'OF.
    /// </summary>
    public interface IExecEncfRepository
    {
        Task<ExecControleOf?> GetExecutionAsync(Guid execControleOfId);
        /// <summary>Exécution non clôturée de l'OF ; pour un OF d'assemblage, le poste doit lui être affecté.</summary>
        Task<ExecControleOf?> GetExecutionOuverteAsync(string numeroOf, string posteCode);
        /// <summary>Formulaire des résultats en cours de fabrication le plus précis pour l'article de l'OF et ce poste, ou null.</summary>
        Task<Guid?> TrouverFormulaireAsync(string? codeArticle, string? posteCode);
        /// <summary>
        /// Plan dont la fiche affiche les lignes : plan de fabrication suivi par l'OF, sinon plan d'assemblage du poste
        /// (celui appliqué dans l'OF, sinon celui de l'article ou de sa famille), ou null.
        /// </summary>
        Task<Guid?> TrouverPlanAsync(ExecControleOf execution, string? posteCode);
        void AddStatut(ExecControleDocumentStatut statut);
        void AddTranche(ExecControleTranche tranche);
        void AddPieceType(ExecPieceType pieceType);
        Task SaveChangesAsync();
    }
}
