using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using SopalTrace.Application.Alertes;
using SopalTrace.Application.DTOs.Execution;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Application.Services;
using SopalTrace.Application.Services.Execution;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;

namespace SopalTrace.Application.Tests.Services
{
    /// <summary>
    /// Saisie des contrôles de lot du plan d'assemblage : LOT_POSTE (première pièce à chaque équipe, dernière
    /// pièce en fin de poste), LOT_OF (première pièce pour la première équipe, dernière pièce en fin d'OF).
    /// </summary>
    public class ExecPlanAssemblageServiceTests
    {
        private const string Poste = "PAS71";

        private readonly Guid _execId = Guid.NewGuid();
        private readonly Guid _planId = Guid.NewGuid();
        private readonly ExecControleOfSession _session;
        private readonly ExecControleDocumentStatut _statut;
        private readonly ExecControleOf _execOf;
        private readonly DocumentSection _lotPoste;
        private readonly DocumentSection _lotOf;
        private readonly DocumentSection _production;
        private readonly DocumentSection _reglage;
        private readonly DocumentSection _toutesPieces;
        private readonly Mock<IExecPlanAssemblageRepository> _repository = new();
        private readonly ExecPlanAssemblageService _service;
        private readonly List<ExecControleLigneReponse> _reponsesAjoutees = new();
        private Guid _premiereSession;

        public ExecPlanAssemblageServiceTests()
        {
            _execOf = new ExecControleOf { Id = _execId, Statut = StatutsExecution.EnCours };
            _execOf.ExecControleOfPostes.Add(new ExecControleOfPoste { PosteCode = Poste });
            _session = new ExecControleOfSession { Id = Guid.NewGuid(), ExecControleOfId = _execId, Equipe = "E1", Statut = StatutsSession.EnCours };
            _premiereSession = _session.Id;
            _statut = new ExecControleDocumentStatut
            {
                Id = Guid.NewGuid(), ExecControleOfId = _execId, PosteCode = Poste, DocId = _planId,
                SessionId = _session.Id, TypeDocument = TypesDocumentExecution.PlanAssemblage
            };

            _lotPoste = Section(SectionTypes.LotPoste, 1);
            _lotOf = Section(SectionTypes.LotOf, 2);
            _production = Section(SectionTypes.Echantillonnage2859, 3);
            _reglage = Section(SectionTypes.Reglage, 4);
            _toutesPieces = Section(SectionTypes.TousPieces, 5);
            var plan = new DocumentEntete { Id = _planId, Nom = "Plan", TypeDocumentCode = TypesDocument.PlanAssemblage };
            foreach (var s in new[] { _lotPoste, _lotOf, _production, _reglage, _toutesPieces }) plan.DocumentSections.Add(s);

            _repository.Setup(r => r.GetExecOfAsync(_execId)).ReturnsAsync(_execOf);
            _repository.Setup(r => r.GetActiveSessionAsync(_execId)).ReturnsAsync(_session);
            _repository.Setup(r => r.GetPremiereSessionIdAsync(_execId)).ReturnsAsync(() => _premiereSession);
            _repository.Setup(r => r.GetStatutPlanAssAsync(_execId, Poste, _session.Id)).ReturnsAsync(_statut);
            _repository.Setup(r => r.GetDocumentByIdAsync(_planId)).ReturnsAsync(plan);
            _repository.Setup(r => r.GetReponsesPlanAsync(_execId, Poste)).ReturnsAsync(() => _reponsesAjoutees
                .Select(r => new ReponseLot(r.DocumentLigneId, r.Contexte, r.ResultatType == ResultatsLigne.NonConforme, r.DateSaisie, _session.Id, r.Equipe, r.DetailsNc, r.ActionsCorrection))
                .ToList());
            _repository.Setup(r => r.AddLigneReponses(It.IsAny<IEnumerable<ExecControleLigneReponse>>()))
                .Callback<IEnumerable<ExecControleLigneReponse>>(r => _reponsesAjoutees.AddRange(r));

            _service = new ExecPlanAssemblageService(_repository.Object);
        }

        private static DocumentSection Section(string type, int ordre)
        {
            var section = new DocumentSection
            {
                Id = Guid.NewGuid(), OrdreAffiche = ordre, LibelleSection = type,
                TypeSection = new TypeSection { TypeNormalise = type, Libelle = type }
            };
            section.DocumentLignes.Add(new DocumentLigne { Id = Guid.NewGuid(), SectionId = section.Id, OrdreAffiche = 1, LibelleAffiche = "Étanchéité" });
            return section;
        }

        private static SaveResultatAssRequest Demande(DocumentSection section, string moment, string resultat = "C", string? nc = null) => new()
        {
            PosteCode = Poste,
            SectionId = section.Id,
            Moment = moment,
            LignesControle = section.DocumentLignes.Select(l => new SaveResultatAssLigneDto { LigneId = l.Id, Resultat = resultat, Nc = nc }).ToList()
        };

        [Fact]
        public async Task PremierePiece_EnregistreeSurLeStatutDuPosteEtLEquipe()
        {
            await _service.SaveResultatAsync(_execId, Demande(_lotPoste, ContextesReponse.PremierePiece, "NC", "Fuite"), "M1");

            var reponse = Assert.Single(_reponsesAjoutees);
            Assert.Equal(ContextesReponse.PremierePiece, reponse.Contexte);
            Assert.Equal(_statut.Id, reponse.ExecControleDocumentStatutId);
            Assert.Equal(("M1", "E1", "NON_CONFORME", "Fuite"), (reponse.MatriculeOperateur, reponse.Equipe, reponse.ResultatType, reponse.DetailsNc));
            // Première équipe : le début d'OF reste à faire.
            Assert.False(_statut.EstDemarrageTermine);
        }

        [Fact]
        public async Task PremiereEquipe_DemarrageTermineApresDebutDePosteEtDebutDOf()
        {
            await _service.SaveResultatAsync(_execId, Demande(_lotPoste, ContextesReponse.PremierePiece), "M1");
            await _service.SaveResultatAsync(_execId, Demande(_lotOf, ContextesReponse.PremierePiece), "M1");

            Assert.True(_statut.EstDemarrageTermine);
            Assert.False(_statut.EstTermine);
        }

        [Fact]
        public async Task EquipeSuivante_DebutDOfRefuseEtDebutDePosteSuffit()
        {
            _premiereSession = Guid.NewGuid();

            await Assert.ThrowsAsync<RegleMetierException>(() => _service.SaveResultatAsync(_execId, Demande(_lotOf, ContextesReponse.PremierePiece), "M1"));
            await _service.SaveResultatAsync(_execId, Demande(_lotPoste, ContextesReponse.PremierePiece), "M1");

            Assert.True(_statut.EstDemarrageTermine);
        }

        [Fact]
        public async Task DernierePiece_RefuseeAvantLaDemandeDeFin()
        {
            await Assert.ThrowsAsync<RegleMetierException>(() => _service.SaveResultatAsync(_execId, Demande(_lotPoste, ContextesReponse.DernierePiece), "M1"));
            Assert.Empty(_reponsesAjoutees);
        }

        [Fact]
        public async Task FinDePoste_DernierePieceDuPosteTermineLeStatut()
        {
            _session.ClosingRequest = DemandesCloture.FinPoste;

            await Assert.ThrowsAsync<RegleMetierException>(() => _service.SaveResultatAsync(_execId, Demande(_lotOf, ContextesReponse.DernierePiece), "M1"));
            await _service.SaveResultatAsync(_execId, Demande(_lotPoste, ContextesReponse.DernierePiece), "M1");

            Assert.True(_statut.EstTermine);
        }

        [Fact]
        public async Task FinDOf_DernierePieceDOfAussiAttendue()
        {
            _session.ClosingRequest = DemandesCloture.FinOf;

            await _service.SaveResultatAsync(_execId, Demande(_lotPoste, ContextesReponse.DernierePiece), "M1");
            Assert.False(_statut.EstTermine);

            await _service.SaveResultatAsync(_execId, Demande(_lotOf, ContextesReponse.DernierePiece), "M1");
            Assert.True(_statut.EstTermine);
        }

        [Fact]
        public async Task ControlesALaDemande_ReglageEtToutesLesPieces_EnregistresSansToucherAuDemarrage()
        {
            await _service.SaveResultatAsync(_execId, Demande(_reglage, ContextesReponse.Reglage), "M1");
            await _service.SaveResultatAsync(_execId, Demande(_toutesPieces, ContextesReponse.CentPourCent), "M1");

            Assert.Equal(new[] { ContextesReponse.Reglage, ContextesReponse.CentPourCent }, _reponsesAjoutees.Select(r => r.Contexte));
            // Une saisie de réglage porte son numéro (contrainte CK_NumeroReglageObligatoire).
            Assert.StartsWith("REG-", _reponsesAjoutees[0].NumeroReglage);
            Assert.Null(_reponsesAjoutees[1].NumeroReglage);
            Assert.False(_statut.EstDemarrageTermine);
        }

        [Fact]
        public async Task ControleALaDemande_SurUneAutreSection_Refuse()
        {
            await Assert.ThrowsAsync<RegleMetierException>(() => _service.SaveResultatAsync(_execId, Demande(_lotPoste, ContextesReponse.Reglage), "M1"));
            await Assert.ThrowsAsync<RegleMetierException>(() => _service.SaveResultatAsync(_execId, Demande(_reglage, ContextesReponse.CentPourCent), "M1"));
            await Assert.ThrowsAsync<RegleMetierException>(() => _service.SaveResultatAsync(_execId, Demande(_reglage, ContextesReponse.PremierePiece), "M1"));
        }

        [Fact]
        public async Task Ressaisie_RepartDesDernieresReponsesDeChaqueLigne()
        {
            _repository.Setup(r => r.GetExecOfAsync(_execId)).ReturnsAsync(new ExecControleOf
            {
                Id = _execId, Statut = StatutsExecution.EnCours, NumeroOf = "OF1",
                NumeroOfNavigation = new MfgheadOrdreFabrication { NumeroOf = "OF1", CodeArticle = "ART" },
                ExecControleOfPostes = { new ExecControleOfPoste { PosteCode = Poste } }
            });
            var demande = Demande(_lotPoste, ContextesReponse.PremierePiece, "NC", "Fuite");
            demande.LignesControle[0].ActionCorrective = "Joint changé";
            await _service.SaveResultatAsync(_execId, demande, "M1");

            var dto = await _service.GetOrInitExecutionAsync(_execId, Poste);

            var ligne = Assert.Single(dto.Sections.Single(s => s.Id == _lotPoste.Id).PremierePiece.Lignes);
            Assert.Equal(("NC", "Fuite", "Joint changé"), (ligne.Resultat, ligne.Nc, ligne.ActionCorrective));
        }

        [Fact]
        public async Task SectionDeProduction_SeSaisitParLesAlertes()
        {
            await Assert.ThrowsAsync<RegleMetierException>(() => _service.SaveResultatAsync(_execId, Demande(_production, ContextesReponse.PremierePiece), "M1"));
        }

        [Fact]
        public async Task LignesIncompletesOuHorsSection_Refusees()
        {
            var incomplete = Demande(_lotPoste, ContextesReponse.PremierePiece);
            incomplete.LignesControle[0].LigneId = Guid.NewGuid();

            await Assert.ThrowsAsync<RegleMetierException>(() => _service.SaveResultatAsync(_execId, incomplete, "M1"));
            _repository.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task NonConformeSansDetail_Refuse()
        {
            await Assert.ThrowsAsync<RegleMetierException>(() => _service.SaveResultatAsync(_execId, Demande(_lotPoste, ContextesReponse.PremierePiece, "NC"), "M1"));
        }

        [Fact]
        public async Task OfClotureOuEnPause_Refuse()
        {
            _session.Statut = StatutsSession.EnPause;
            await Assert.ThrowsAsync<EtatExecutionInvalideException>(() => _service.SaveResultatAsync(_execId, Demande(_lotPoste, ContextesReponse.PremierePiece), "M1"));

            _execOf.Statut = StatutsExecution.Cloture;
            await Assert.ThrowsAsync<EtatExecutionInvalideException>(() => _service.SaveResultatAsync(_execId, Demande(_lotPoste, ContextesReponse.PremierePiece), "M1"));
        }

        [Fact]
        public async Task SansMatricule_Refuse()
        {
            // Un matricule inconnu est refusé par la base (clé étrangère vers l'annuaire AUTILIS).
            await Assert.ThrowsAsync<RegleMetierException>(() => _service.SaveResultatAsync(_execId, Demande(_lotPoste, ContextesReponse.PremierePiece), ""));
        }

        [Fact]
        public async Task Lecture_NeRetourneQueLesSectionsDeLotAvecCeQuiEstAttendu()
        {
            _premiereSession = Guid.NewGuid();
            _repository.Setup(r => r.GetExecOfAsync(_execId)).ReturnsAsync(new ExecControleOf
            {
                Id = _execId, Statut = StatutsExecution.EnCours, NumeroOf = "OF1",
                NumeroOfNavigation = new MfgheadOrdreFabrication { NumeroOf = "OF1", CodeArticle = "ART" },
                ExecControleOfPostes = { new ExecControleOfPoste { PosteCode = Poste } }
            });

            var dto = await _service.GetOrInitExecutionAsync(_execId, Poste);

            Assert.Equal(new[] { SectionTypes.LotPoste, SectionTypes.LotOf }, dto.Sections.Select(s => s.TypeSection));
            Assert.Equal(new[] { ContextesReponse.Reglage, ContextesReponse.CentPourCent }, dto.SectionsALaDemande.Select(s => s.Contexte));
            Assert.False(dto.PremiereEquipe);
            Assert.True(dto.Sections[0].PremierePiece.Requis);
            Assert.False(dto.Sections[1].PremierePiece.Requis);
            // Équipe suivante sans section de début d'OF à faire : le démarrage ne dépend que du début de poste.
            Assert.False(_statut.EstDemarrageTermine);
        }
    }

    public class AlertePlanManquantDefinitionTests
    {
        [Fact]
        public void ConstruireCorps_EncodeLesSaisiesOperateur()
        {
            var definition = new AlertePlanManquantDefinition(Mock.Of<IUnitOfWork>());
            var contexte = new PlanManquantContexte
            {
                OperationCode = "ASS",
                PosteCode = "PAS71",
                NomOperateur = "Op",
                DescriptionProbleme = "<script>alert(1)</script>"
            };

            var corps = definition.ConstruireCorps(contexte);

            Assert.DoesNotContain("<script>", corps);
            Assert.Contains("&lt;script&gt;", corps);
        }

        [Fact]
        public void ConstruireSujet_SupprimeLesRetoursALaLigne()
        {
            var definition = new AlertePlanManquantDefinition(Mock.Of<IUnitOfWork>());
            var sujet = definition.ConstruireSujet(new PlanManquantContexte
            {
                OperationCode = "ASS",
                PosteCode = "PAS71\r\nBcc: pirate@exemple.com",
                DescriptionProbleme = "x"
            });

            Assert.DoesNotContain('\n', sujet);
            Assert.DoesNotContain('\r', sujet);
        }
    }
}
