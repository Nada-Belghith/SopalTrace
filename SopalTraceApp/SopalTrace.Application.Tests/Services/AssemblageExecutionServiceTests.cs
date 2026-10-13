using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using SopalTrace.Application.DTOs.Execution.Operateur;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Application.Services.Execution;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;

namespace SopalTrace.Application.Tests.Services
{
    public class AssemblageExecutionServiceTests
    {
        private const string NumeroOf = "OF001";

        private readonly Mock<IOperateurRepository> _repository = new();
        private readonly Mock<IOccurrenceService> _occurrenceService = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = UnitOfWorkMock.Transactionnelle();
        private readonly Mock<IDemarragePosteRepository> _demarrages = new();
        private readonly AssemblageExecutionService _service;

        public AssemblageExecutionServiceTests()
        {
            _service = new AssemblageExecutionService(_repository.Object, _occurrenceService.Object, _unitOfWork.Object, _demarrages.Object);
            _demarrages.Setup(d => d.GetDemarragesAsync(It.IsAny<Guid>())).ReturnsAsync(new List<DemarragePoste>());
            _repository.Setup(r => r.GetOfAsync(NumeroOf)).ReturnsAsync(new MfgheadOrdreFabrication { NumeroOf = NumeroOf, CodeArticle = "ART" });
            _repository.Setup(r => r.OfAssemblageDisponibleAsync(NumeroOf)).ReturnsAsync(true);
            _repository.Setup(r => r.GetCodesPostesActifsAsync(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(new HashSet<string> { "PAS71" });
        }

        private static DemarrerOfAssemblageRequest Demande(params string[] postes) =>
            new() { NumeroOf = NumeroOf, Equipe = "E1", PosteCodes = new List<string>(postes) };

        [Fact]
        public async Task DemarrerOfAssemblage_DejaDemarre_Refuse()
        {
            _repository.Setup(r => r.ExisteExecutionAssemblageActiveAsync(NumeroOf)).ReturnsAsync(true);

            await Assert.ThrowsAsync<EtatExecutionInvalideException>(() => _service.DemarrerOfAssemblageAsync(Demande("PAS71"), "M1"));
            _repository.Verify(r => r.AddExecControleOf(It.IsAny<ExecControleOf>()), Times.Never);
        }

        [Fact]
        public async Task DemarrerOfAssemblage_PosteInconnu_Refuse()
        {
            await Assert.ThrowsAsync<RegleMetierException>(() => _service.DemarrerOfAssemblageAsync(Demande("PAS71", "INCONNU"), "M1"));
            _repository.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task DemarrerOfAssemblage_OfNonPrepare_Refuse()
        {
            _repository.Setup(r => r.OfAssemblageDisponibleAsync(NumeroOf)).ReturnsAsync(false);

            await Assert.ThrowsAsync<RegleMetierException>(() => _service.DemarrerOfAssemblageAsync(Demande("PAS71"), "M1"));
        }

        [Fact]
        public async Task DemarrerOfAssemblage_Valide_CreeExecutionSessionEtPostes()
        {
            ExecControleOf? cree = null;
            _repository.Setup(r => r.AddExecControleOf(It.IsAny<ExecControleOf>())).Callback<ExecControleOf>(e => cree = e);

            var dto = await _service.DemarrerOfAssemblageAsync(Demande("PAS71", "PAS71"), "M1");

            Assert.NotNull(cree);
            Assert.Equal(TypesOf.Assemblage, cree!.TypeOf);
            Assert.Equal(StatutsExecution.EnCours, dto.Statut);
            Assert.Single(cree.ExecControleOfPostes);
            var session = Assert.Single(cree.ExecControleOfSessions);
            Assert.Equal("E1", session.Equipe);
            _repository.Verify(r => r.SaveChangesAsync(), Times.Once);
            _unitOfWork.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<Guid>>>()), Times.Once);
        }

        [Fact]
        public async Task DemarrerOfAssemblage_EchecEnregistrement_ErreurRemonteeDepuisLaTransaction()
        {
            _repository.Setup(r => r.SaveChangesAsync()).ThrowsAsync(new InvalidOperationException("panne"));

            // L'exception traverse ExecuteInTransactionAsync, qui annule l'enregistrement de l'OF.
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DemarrerOfAssemblageAsync(Demande("PAS71"), "M1"));
            _unitOfWork.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<Guid>>>()), Times.Once);
        }

        /// <summary>Document de vérification machine de l'équipe qui tient le poste.</summary>
        private ExecControleDocumentStatut DocumentTenu(Guid statutId, string? machine = null)
        {
            var sessionId = Guid.NewGuid();
            _demarrages.Setup(d => d.EstEnCoursAsync(sessionId, "PAS71")).ReturnsAsync(true);
            return new ExecControleDocumentStatut
            {
                Id = statutId, SessionId = sessionId, PosteCode = "PAS71", MachineCode = machine,
                ExecControleOf = new ExecControleOf { Statut = StatutsExecution.EnCours }
            };
        }

        [Fact]
        public async Task MarquerDocumentTermine_DocumentDUneEquipeQuiNeTientPlusLePoste_Refuse()
        {
            var statutId = Guid.NewGuid();
            var execId = Guid.NewGuid();
            _repository.Setup(r => r.GetDocumentStatutByIdAsync(statutId)).ReturnsAsync(new ExecControleDocumentStatut
            {
                Id = statutId,
                ExecControleOfId = execId,
                SessionId = Guid.NewGuid(),
                PosteCode = "PAS71",
                ExecControleOf = new ExecControleOf { Id = execId, Statut = StatutsExecution.EnCours }
            });

            await Assert.ThrowsAsync<EtatExecutionInvalideException>(() => _service.MarquerDocumentTermineAsync(statutId, "M1", null));
            _repository.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task RouvrirDocument_EnregistreLeMatriculeEtRouvre()
        {
            var statutId = Guid.NewGuid();
            var statut = DocumentTenu(statutId);
            statut.EstDemarrageTermine = true;
            statut.EstTermine = true;
            statut.DateTermine = DateTime.Now;
            statut.MatriculeOperateur = "M1";
            _repository.Setup(r => r.GetDocumentStatutByIdAsync(statutId)).ReturnsAsync(statut);

            await _service.RouvrirDocumentAsync(statutId, "M2", null);

            Assert.False(statut.EstDemarrageTermine);
            Assert.False(statut.EstTermine);
            Assert.Null(statut.DateTermine);
            Assert.Equal("M2", statut.MatriculeOperateur);
            _repository.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        private Guid OfEnCours(string statut = StatutsExecution.EnCours)
        {
            var execId = Guid.NewGuid();
            _repository.Setup(r => r.GetExecOfByIdAsync(execId)).ReturnsAsync(new ExecControleOf { Id = execId, Statut = statut });
            _repository.Setup(r => r.GetPostesForExecutionAsync(execId)).ReturnsAsync(new List<string> { "PAS71", "PAS72" });
            _repository.Setup(r => r.GetCodesPostesActifsAsync(It.IsAny<IEnumerable<string>>())).ReturnsAsync(new HashSet<string> { "PAS71", "PAS72" });
            return execId;
        }

        private ExecControleOfSession EquipeOuverte(Guid execId, string equipe, string statut, params DemarragePoste[] postes)
        {
            var session = new ExecControleOfSession { Id = Guid.NewGuid(), ExecControleOfId = execId, Equipe = equipe, Statut = statut };
            _ouvertes.Add(session);
            _postesOf.AddRange(postes.Select(p => p with { SessionId = session.Id, Equipe = equipe }));
            _demarrages.Setup(d => d.GetSessionsOuvertesAsync(execId)).ReturnsAsync(() => _ouvertes.ToList());
            _demarrages.Setup(d => d.GetDemarragesAsync(execId)).ReturnsAsync(() => _postesOf.ToList());
            return session;
        }

        private readonly List<ExecControleOfSession> _ouvertes = new();
        private readonly List<DemarragePoste> _postesOf = new();
        private static DemarragePoste Tenu(string poste, DateTime? fin = null) => new(Guid.Empty, poste, DateTime.Now.AddHours(-2), EndedAt: fin);

        [Fact]
        public async Task AjouterPostes_PosteTenuParUneAutreEquipe_RefuseTantQuElleNeLAPasTermine()
        {
            var execId = OfEnCours();
            EquipeOuverte(execId, "E2", StatutsSession.EnCours, Tenu("PAS71"));

            var ex = await Assert.ThrowsAsync<EtatExecutionInvalideException>(() =>
                _service.AjouterPostesAsync(execId, new AjouterPostesRequest { Equipe = "E1", PosteCodes = new List<string> { "PAS71" } }, "M1"));
            Assert.Contains("E2", ex.Message);
            Assert.Contains("PAS71", ex.Message);
        }

        [Fact]
        public async Task AjouterPostes_AutreEquipeSurUnAutrePoste_LEquipePrendSonPoste()
        {
            var execId = OfEnCours();
            EquipeOuverte(execId, "E1", StatutsSession.EnCours, Tenu("PAS72"), Tenu("PAS71", fin: DateTime.Now.AddMinutes(-10)));
            ExecControleOfSession? ajoutee = null;
            _repository.Setup(r => r.AddSession(It.IsAny<ExecControleOfSession>())).Callback<ExecControleOfSession>(s => ajoutee = s);
            var pris = new List<ExecSessionPost>();
            _demarrages.Setup(d => d.Ajouter(It.IsAny<ExecSessionPost>())).Callback<ExecSessionPost>(pris.Add);

            await _service.AjouterPostesAsync(execId, new AjouterPostesRequest { Equipe = "E2", PosteCodes = new List<string> { "PAS71" } }, "M1");

            Assert.Equal("E2", ajoutee?.Equipe);
            var poste = Assert.Single(pris);
            Assert.Equal((ajoutee!.Id, execId, "PAS71"), (poste.SessionId, poste.ExecControleOfId, poste.PosteCode));
        }

        [Fact]
        public async Task AjouterPostes_PosteDejaTermineDansLaPriseDePoste_Refuse()
        {
            var execId = OfEnCours();
            EquipeOuverte(execId, "E1", StatutsSession.EnCours, Tenu("PAS72"), Tenu("PAS71", fin: DateTime.Now.AddMinutes(-10)));

            var ex = await Assert.ThrowsAsync<EtatExecutionInvalideException>(() =>
                _service.AjouterPostesAsync(execId, new AjouterPostesRequest { Equipe = "E1", PosteCodes = new List<string> { "PAS71" } }, "M1"));
            Assert.Contains("déjà terminé", ex.Message);
        }

        [Fact]
        public async Task AjouterPostes_EquipeEnPause_DoitReprendreDAbord()
        {
            var execId = OfEnCours(StatutsExecution.EnPause);
            EquipeOuverte(execId, "E1", StatutsSession.EnPause, Tenu("PAS72"));

            var ex = await Assert.ThrowsAsync<EtatExecutionInvalideException>(() =>
                _service.AjouterPostesAsync(execId, new AjouterPostesRequest { Equipe = "E1", PosteCodes = new List<string> { "PAS71" } }, "M1"));
            Assert.Contains("pause", ex.Message);
        }

        [Fact]
        public async Task AjouterPostes_PendantLaClotureDUneAutreEquipe_Refuse()
        {
            var execId = OfEnCours();
            var e1 = EquipeOuverte(execId, "E1", StatutsSession.EnCours, Tenu("PAS72"));
            e1.ClosingRequest = DemandesCloture.FinOf;

            var ex = await Assert.ThrowsAsync<EtatExecutionInvalideException>(() =>
                _service.AjouterPostesAsync(execId, new AjouterPostesRequest { Equipe = "E2", PosteCodes = new List<string> { "PAS71" } }, "M1"));
            Assert.Contains("clôture", ex.Message);
        }

        [Fact]
        public async Task AjouterPostes_PourCloturer_AlorsQuUneAutreEquipeTientUnPoste_Refuse()
        {
            var execId = OfEnCours();
            EquipeOuverte(execId, "E1", StatutsSession.EnCours, Tenu("PAS72"));

            var ex = await Assert.ThrowsAsync<EtatExecutionInvalideException>(() =>
                _service.AjouterPostesAsync(execId, new AjouterPostesRequest { Equipe = "E2", PourCloturer = true }, "M1"));
            Assert.Contains("E1 tient encore PAS72", ex.Message);
            _repository.Verify(r => r.AddSession(It.IsAny<ExecControleOfSession>()), Times.Never);
        }

        [Fact]
        public async Task AjouterPostes_OfLibre_NouvelleSessionEtPostesChoisisDemarres()
        {
            var execId = OfEnCours();
            _demarrages.Setup(d => d.GetSessionsOuvertesAsync(execId)).ReturnsAsync(new List<ExecControleOfSession>());
            ExecControleOfSession? ajoutee = null;
            _repository.Setup(r => r.AddSession(It.IsAny<ExecControleOfSession>())).Callback<ExecControleOfSession>(s => ajoutee = s);
            var demarres = new List<ExecSessionPost>();
            _demarrages.Setup(d => d.Ajouter(It.IsAny<ExecSessionPost>())).Callback<ExecSessionPost>(demarres.Add);

            await _service.AjouterPostesAsync(execId, new AjouterPostesRequest { Equipe = "E2", PosteCodes = new List<string> { "PAS71" } }, "M1");

            Assert.Equal(("E2", StatutsSession.EnCours, (string?)null), (ajoutee?.Equipe, ajoutee?.Statut, ajoutee?.ClosingRequest));
            var demarrage = Assert.Single(demarres);
            Assert.Equal((ajoutee!.Id, "PAS71", (string?)"M1"), (demarrage.SessionId, demarrage.PosteCode, demarrage.StartedBy));
        }

        [Fact]
        public async Task AjouterPostes_PourCloturer_SessionEnFinDOfSansPosteDemarre()
        {
            var execId = OfEnCours();
            _demarrages.Setup(d => d.GetSessionsOuvertesAsync(execId)).ReturnsAsync(new List<ExecControleOfSession>());
            ExecControleOfSession? ajoutee = null;
            _repository.Setup(r => r.AddSession(It.IsAny<ExecControleOfSession>())).Callback<ExecControleOfSession>(s => ajoutee = s);

            await _service.AjouterPostesAsync(execId, new AjouterPostesRequest { Equipe = "E3", PourCloturer = true }, "M1");

            Assert.Equal(DemandesCloture.FinOf, ajoutee?.ClosingRequest);
            _demarrages.Verify(d => d.Ajouter(It.IsAny<ExecSessionPost>()), Times.Never);
        }

        [Fact]
        public async Task MarquerDocumentTermine_OfCloture_Refuse()
        {
            var statutId = Guid.NewGuid();
            _repository.Setup(r => r.GetDocumentStatutByIdAsync(statutId)).ReturnsAsync(new ExecControleDocumentStatut
            {
                Id = statutId,
                ExecControleOf = new ExecControleOf { Statut = StatutsExecution.Cloture }
            });

            await Assert.ThrowsAsync<EtatExecutionInvalideException>(() => _service.MarquerDocumentTermineAsync(statutId, "M1", null));
        }

        [Fact]
        public async Task MarquerDocumentTermine_MachineSansPauseNiFin_TermineAuDemarrage()
        {
            var statutId = Guid.NewGuid();
            var statut = DocumentTenu(statutId, "MAS19");
            _repository.Setup(r => r.GetDocumentStatutByIdAsync(statutId)).ReturnsAsync(statut);
            _repository.Setup(r => r.GetPeriodicitesPlanMachineAsync("MAS19")).ReturnsAsync(new List<string> { "démarrage" });

            await _service.MarquerDocumentTermineAsync(statutId, "M1", "demarrage");

            Assert.True(statut.EstDemarrageTermine);
            Assert.True(statut.EstTermine);
            Assert.Equal("M1", statut.MatriculeOperateur);
        }
    }
}
