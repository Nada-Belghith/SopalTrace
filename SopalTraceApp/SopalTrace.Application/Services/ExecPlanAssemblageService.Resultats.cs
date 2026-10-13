using SopalTrace.Application.DTOs.Execution;
using SopalTrace.Application.Services.Execution;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Application.Services
{
    public partial class ExecPlanAssemblageService
    {
        /// <summary>
        /// Enregistre le contrôle (première ou dernière pièce) d'une section de lot. Chaque saisie est ajoutée,
        /// jamais écrasée (historique exigé par la traçabilité qualité) ; l'état retient la dernière saisie.
        /// </summary>
        public async Task SaveResultatAsync(Guid execControleOfId, SaveResultatAssRequest request, string matricule)
        {
            if (string.IsNullOrWhiteSpace(matricule))
                throw new RegleMetierException("Utilisateur connecté introuvable. Veuillez vous reconnecter.");

            var execOf = await GetExecOfAsync(execControleOfId);
            if (execOf.Statut == StatutsExecution.Cloture)
                throw new EtatExecutionInvalideException("Cet OF est clôturé : aucune saisie n'est possible.");

            var session = await _repository.GetActiveSessionAsync(execControleOfId)
                ?? throw new EtatExecutionInvalideException("Aucune équipe n'est active sur cet OF.");
            if (session.Statut == StatutsSession.EnPause)
                throw new EtatExecutionInvalideException("L'OF est en pause : reprenez la production avant de saisir un contrôle.");

            var statut = await _repository.GetStatutPlanAssAsync(execControleOfId, request.PosteCode, session.Id);
            if (statut?.DocId == null)
                throw new RegleMetierException("Ouvrez le plan d'assemblage du poste avant de saisir un résultat.");
            var plan = await _repository.GetDocumentByIdAsync(statut.DocId.Value)
                ?? throw new RessourceIntrouvableException("Le plan d'assemblage associé à cette exécution n'existe plus.");

            var section = plan.DocumentSections.FirstOrDefault(s => s.Id == request.SectionId)
                ?? throw new RegleMetierException("Cette section ne fait pas partie du plan d'assemblage du poste.");
            var type = TypeDe(section);
            var aLaDemande = request.Moment is ContextesReponse.Reglage or ContextesReponse.CentPourCent;
            if (aLaDemande ? ControlesLot.ContexteALaDemande(type) != request.Moment : !ControlesLot.EstSectionLot(type))
                throw new RegleMetierException(ControlesLot.EstSectionLot(type) || ControlesLot.ContexteALaDemande(type) != null
                    ? "Ce type de contrôle ne correspond pas à la section choisie."
                    : "Les contrôles de production se saisissent depuis les alertes de production.");

            var lignesSection = section.DocumentLignes.Select(l => l.Id).ToHashSet();
            var lignesSaisies = request.LignesControle.Select(l => l.LigneId).ToList();
            if (lignesSaisies.Count != lignesSaisies.Distinct().Count() || !lignesSection.SetEquals(lignesSaisies))
                throw new RegleMetierException("Toutes les caractéristiques de la section doivent être saisies, une seule fois chacune.");

            if (request.LignesControle.Any(l => l.Resultat == ResultatsControle.NonConforme && string.IsNullOrWhiteSpace(l.Nc)))
                throw new RegleMetierException("Le détail de la non-conformité est obligatoire pour chaque ligne NC.");

            var equipe = await ContexteEquipeAsync(execControleOfId, session);
            if (!aLaDemande) VerifierMomentAttendu(type, request.Moment, equipe);

            var maintenant = DateTime.Now;
            var nouvelles = request.LignesControle.Select(l => new ExecControleLigneReponse
            {
                Id = Guid.NewGuid(),
                ExecControleOfid = execControleOfId,
                ExecControleDocumentStatutId = statut.Id,
                DocumentLigneId = l.LigneId,
                Contexte = request.Moment,
                // Une saisie de réglage porte son numéro (contrainte CK_NumeroReglageObligatoire).
                NumeroReglage = request.Moment == ContextesReponse.Reglage ? $"REG-{maintenant:HHmmss}" : null,
                ResultatType = l.Resultat == ResultatsControle.NonConforme ? ResultatsLigne.NonConforme : ResultatsLigne.Conforme,
                DetailsNc = l.Resultat == ResultatsControle.NonConforme ? l.Nc?.Trim() : null,
                ActionsCorrection = l.Resultat == ResultatsControle.NonConforme ? l.ActionCorrective?.Trim() : null,
                MatriculeOperateur = matricule,
                Equipe = session.Equipe,
                DateSaisie = maintenant
            }).ToList();
            _repository.AddLigneReponses(nouvelles);

            var reponses = (await _repository.GetReponsesPlanAsync(execControleOfId, request.PosteCode))
                .Concat(nouvelles.Select(r => new ReponseLot(r.DocumentLigneId, r.Contexte, r.ResultatType == ResultatsLigne.NonConforme,
                    r.DateSaisie, session.Id, r.Equipe, r.DetailsNc, r.ActionsCorrection)))
                .ToList();
            SynchroniserAvancement(statut, SectionsLot(plan, equipe, reponses), equipe);
            statut.MatriculeOperateur = matricule;

            await _repository.SaveChangesAsync();
        }

        private static void VerifierMomentAttendu(string typeSection, string moment, ContexteEquipe equipe)
        {
            if (moment == ContextesReponse.PremierePiece && !ControlesLot.PremierePieceRequise(typeSection, equipe))
                throw new RegleMetierException("L'OF a déjà été démarré par une équipe précédente : le contrôle de début d'OF n'est plus à faire.");

            if (moment == ContextesReponse.DernierePiece && !ControlesLot.DernierePieceRequise(typeSection, equipe))
                throw new RegleMetierException(typeSection == SectionTypes.LotOf
                    ? "Le contrôle de fin d'OF se fait après avoir demandé la clôture de l'OF."
                    : "Le contrôle de fin de poste se fait après avoir demandé la fin de poste.");
        }
    }
}
