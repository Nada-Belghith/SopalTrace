using SopalTrace.Application.DTOs.Execution;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Application.Services.Execution;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Application.Services
{
    /// <summary>
    /// Plan d'assemblage d'un OF produit fini, poste par poste : contrôles de lot (première pièce au démarrage,
    /// dernière pièce à la fin) de l'équipe active. Règles des lots : <see cref="ControlesLot"/>.
    /// </summary>
    public partial class ExecPlanAssemblageService : IExecPlanAssemblageService
    {
        private readonly IExecPlanAssemblageRepository _repository;
        private readonly IDemarragePosteRepository _demarrages;

        public ExecPlanAssemblageService(IExecPlanAssemblageRepository repository, IDemarragePosteRepository demarrages)
        {
            _repository = repository;
            _demarrages = demarrages;
        }

        public static string MessagePlanIntrouvable(string codeArticle, string posteCode) =>
            $"Aucun plan d'assemblage actif pour la famille de l'article {codeArticle} sur le poste {posteCode}.";

        public async Task<ExecPlanAssemblageDto> GetOrInitExecutionAsync(Guid execControleOfId, string posteCode)
        {
            var execOf = await GetExecOfAsync(execControleOfId);
            VerifierPosteAffecte(execOf, posteCode);
            var codeArticle = execOf.NumeroOfNavigation.CodeArticle;
            // Équipe du poste : celle qui le tient (ou qui clôture l'OF).
            var session = await _demarrages.GetSessionAsync(execControleOfId, posteCode);

            var statut = session == null ? null : await _repository.GetStatutPlanAssAsync(execControleOfId, posteCode, session.Id);
            var planId = statut?.DocId ?? await _repository.TrouverPlanAssemblageIdAsync(codeArticle, posteCode)
                ?? throw new RessourceIntrouvableException(MessagePlanIntrouvable(codeArticle, posteCode));
            var plan = await _repository.GetDocumentByIdAsync(planId)
                ?? throw new RessourceIntrouvableException("Le plan d'assemblage associé à cette exécution n'existe plus.");

            var equipe = await ContexteEquipeAsync(execControleOfId, session, posteCode);
            var reponses = await _repository.GetReponsesPlanAsync(execControleOfId, posteCode);
            var sections = SectionsLot(plan, equipe, reponses);

            // Le statut du plan (version figée) naît avec le démarrage du poste, ou pour le contrôle de fin d'OF.
            if (session != null && execOf.Statut != StatutsExecution.Cloture &&
                (equipe.PosteDemarre || equipe.DemandeCloture == DemandesCloture.FinOf))
            {
                statut ??= NouveauStatut(execOf.Id, posteCode, session);
                statut.DocId ??= plan.Id;
                SynchroniserAvancement(statut, sections, equipe);
                await _repository.SaveChangesAsync();
            }

            return new ExecPlanAssemblageDto
            {
                ExecControleOfId = execControleOfId,
                NumeroOf = execOf.NumeroOf,
                CodeArticle = codeArticle,
                DesignationArticle = execOf.NumeroOfNavigation.CodeArticleNavigation?.Designation,
                PosteCode = posteCode,
                Statut = execOf.Statut,
                DateDebut = execOf.DateDebut,
                LibellePlan = plan.Designation ?? plan.Nom,
                PosteDemarre = equipe.PosteDemarre,
                PremierDemarragePoste = equipe.PremierDemarrage,
                DemandeCloture = equipe.DemandeCloture,
                Sections = sections,
                SectionsALaDemande = SectionsALaDemande(plan, equipe, reponses)
            };
        }

        private async Task<ExecControleOf> GetExecOfAsync(Guid execControleOfId)
        {
            return await _repository.GetExecOfAsync(execControleOfId)
                ?? throw new RessourceIntrouvableException("Exécution OF introuvable.");
        }

        private static void VerifierPosteAffecte(ExecControleOf execOf, string posteCode)
        {
            if (!execOf.ExecControleOfPostes.Any(p => p.PosteCode == posteCode))
                throw new RegleMetierException($"Le poste {posteCode} n'est pas affecté à cet OF.");
        }

        private async Task<ContexteEquipe> ContexteEquipeAsync(Guid execControleOfId, ExecControleOfSession? session, string posteCode) =>
            session == null ? ContexteEquipe.SansSession
                : ContexteEquipe.Pour(session, posteCode, await _demarrages.GetDemarragesAsync(execControleOfId));

        /// <summary>Statut du plan pour le poste et l'équipe active : il fige la version du plan utilisée.</summary>
        private ExecControleDocumentStatut NouveauStatut(Guid execControleOfId, string posteCode, ExecControleOfSession session)
        {
            var statut = new ExecControleDocumentStatut
            {
                Id = Guid.NewGuid(),
                ExecControleOfId = execControleOfId,
                TypeDocument = TypesDocumentExecution.PlanAssemblage,
                PosteCode = posteCode,
                SessionId = session.Id,
                Equipe = session.Equipe,
                DateExecution = DateTime.Now
            };
            _repository.AddStatut(statut);
            return statut;
        }

        /// <summary>
        /// Démarrage fait quand toutes les premières pièces attendues le sont (immédiat pour un plan sans
        /// section de lot) ; terminé quand toutes les dernières pièces attendues le sont, après la demande de fin.
        /// </summary>
        private static void SynchroniserAvancement(ExecControleDocumentStatut statut, IReadOnlyCollection<ExecPlanAssemblageSectionDto> sections, ContexteEquipe equipe)
        {
            statut.EstDemarrageTermine = sections.All(s => !s.PremierePiece.Requis || s.PremierePiece.Fait);
            var termine = equipe.DemandeCloture != null && sections.All(s => !s.DernierePiece.Requis || s.DernierePiece.Fait);
            if (termine != statut.EstTermine) statut.DateTermine = termine ? DateTime.Now : null;
            statut.EstTermine = termine;
        }
    }
}
