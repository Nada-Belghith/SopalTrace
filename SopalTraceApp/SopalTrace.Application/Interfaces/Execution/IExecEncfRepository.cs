using System;
using System.Threading.Tasks;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Interfaces.Execution
{
    /// <summary>
    /// Résultat contrôle en cours de fabrication (ENCF) : document RESULTAT_CF exécuté pendant un OF démarré,
    /// sur la même exécution que les autres documents (pièces types et résultats par tranche de l'OF).
    /// </summary>
    public interface IExecEncfRepository
    {
        Task<ExecControleOf?> GetExecutionAsync(Guid execControleOfId);
        /// <summary>Exécution non clôturée de l'OF ; pour un OF d'assemblage, le poste doit lui être affecté.</summary>
        Task<ExecControleOf?> GetExecutionOuverteAsync(string numeroOf, string posteCode);
        /// <summary>Document « résultat contrôle en cours de fabrication » actif du poste (ou commun à tous les postes).</summary>
        Task<DocumentEntete?> GetDocumentActifAsync(string posteCode);
        void AddStatut(ExecControleDocumentStatut statut);
        void AddTranche(ExecControleTranche tranche);
        void AddPieceType(ExecPieceType pieceType);
        Task SaveChangesAsync();
    }
}
