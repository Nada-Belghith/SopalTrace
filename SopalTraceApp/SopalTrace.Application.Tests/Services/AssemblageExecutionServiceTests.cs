using System;
using System.Collections.Generic;
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
        private readonly AssemblageExecutionService _service;

        public AssemblageExecutionServiceTests()
        {
            _service = new AssemblageExecutionService(_repository.Object, _occurrenceService.Object, _unitOfWork.Object);
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

            await Assert.ThrowsAsync<EtatExecutionInvalideException>(() => _service.DemarrerOfAssemblageAsync(Demande("PAS71")));
            _repository.Verify(r => r.AddExecControleOf(It.IsAny<ExecControleOf>()), Times.Never);
        }

        [Fact]
        public async Task DemarrerOfAssemblage_PosteInconnu_Refuse()
        {
            await Assert.ThrowsAsync<RegleMetierException>(() => _service.DemarrerOfAssemblageAsync(Demande("PAS71", "INCONNU")));
            _repository.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task DemarrerOfAssemblage_OfNonPrepare_Refuse()
        {
            _repository.Setup(r => r.OfAssemblageDisponibleAsync(NumeroOf)).ReturnsAsync(false);

            await Assert.ThrowsAsync<RegleMetierException>(() => _service.DemarrerOfAssemblageAsync(Demande("PAS71")));
        }

        [Fact]
        public async Task DemarrerOfAssemblage_Valide_CreeExecutionSessionEtPostes()
        {
            ExecControleOf? cree = null;
            _repository.Setup(r => r.AddExecControleOf(It.IsAny<ExecControleOf>())).Callback<ExecControleOf>(e => cree = e);

            var dto = await _service.DemarrerOfAssemblageAsync(Demande("PAS71", "PAS71"));

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
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DemarrerOfAssemblageAsync(Demande("PAS71")));
            _unitOfWork.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<Guid>>>()), Times.Once);
        }

        [Fact]
        public async Task MarquerDocumentTermine_DocumentSessionTerminee_Refuse()
        {
            var statutId = Guid.NewGuid();
            var execId = Guid.NewGuid();
            _repository.Setup(r => r.GetDocumentStatutByIdAsync(statutId)).ReturnsAsync(new ExecControleDocumentStatut
            {
                Id = statutId,
                ExecControleOfId = execId,
                SessionId = Guid.NewGuid(),
                ExecControleOf = new ExecControleOf { Id = execId, Statut = StatutsExecution.EnCours }
            });
            var session = new ExecControleOfSession { Id = Guid.NewGuid(), Statut = StatutsSession.EnCours };
            _repository.Setup(r => r.GetActiveSessionAsync(execId)).ReturnsAsync(session);
            _repository.Setup(r => r.GetSessionsOuvertesAsync(execId)).ReturnsAsync(new List<ExecControleOfSession> { session });

            await Assert.ThrowsAsync<EtatExecutionInvalideException>(() => _service.MarquerDocumentTermineAsync(statutId, "M1", null));
            _repository.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task RouvrirDocument_EnregistreLeMatriculeEtRouvre()
        {
            var statutId = Guid.NewGuid();
            var statut = new ExecControleDocumentStatut
            {
                Id = statutId,
                EstDemarrageTermine = true,
                EstTermine = true,
                DateTermine = DateTime.Now,
                MatriculeOperateur = "M1",
                ExecControleOf = new ExecControleOf { Statut = StatutsExecution.EnCours }
            };
            _repository.Setup(r => r.GetDocumentStatutByIdAsync(statutId)).ReturnsAsync(statut);

            await _service.RouvrirDocumentAsync(statutId, "M2", null);

            Assert.False(statut.EstDemarrageTermine);
            Assert.False(statut.EstTermine);
            Assert.Null(statut.DateTermine);
            Assert.Equal("M2", statut.MatriculeOperateur);
            _repository.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task AjouterPostes_AutreEquipeActive_Refuse()
        {
            var execId = Guid.NewGuid();
            _repository.Setup(r => r.GetExecOfByIdAsync(execId)).ReturnsAsync(new ExecControleOf { Id = execId, Statut = StatutsExecution.EnCours });
            _repository.Setup(r => r.GetSessionsOuvertesAsync(execId)).ReturnsAsync(new List<ExecControleOfSession> { new() { Equipe = "E2", Statut = StatutsSession.EnCours } });

            var ex = await Assert.ThrowsAsync<EtatExecutionInvalideException>(() =>
                _service.AjouterPostesAsync(execId, new AjouterPostesRequest { Equipe = "E1", PosteCodes = new List<string>() }));
            Assert.Contains("E2", ex.Message);
        }

        [Fact]
        public async Task AjouterPostes_AutreEquipeEnPause_NouvelleEquipeEtOfEnProduction()
        {
            var execId = Guid.NewGuid();
            var execOf = new ExecControleOf { Id = execId, Statut = StatutsExecution.EnPause, SuspendedAt = DateTime.Now.AddMinutes(-10) };
            _repository.Setup(r => r.GetExecOfByIdAsync(execId)).ReturnsAsync(execOf);
            _repository.Setup(r => r.GetSessionsOuvertesAsync(execId)).ReturnsAsync(new List<ExecControleOfSession> { new() { Equipe = "E1", Statut = StatutsSession.EnPause } });
            _repository.Setup(r => r.GetPostesForExecutionAsync(execId)).ReturnsAsync(new List<string>());
            ExecControleOfSession? ajoutee = null;
            _repository.Setup(r => r.AddSession(It.IsAny<ExecControleOfSession>())).Callback<ExecControleOfSession>(s => ajoutee = s);

            await _service.AjouterPostesAsync(execId, new AjouterPostesRequest { Equipe = "E2", PosteCodes = new List<string>() });

            Assert.Equal(("E2", StatutsSession.EnCours), (ajoutee?.Equipe, ajoutee?.Statut));
            Assert.Equal(StatutsExecution.EnCours, execOf.Statut);
            Assert.True(execOf.TempsPauseTotalMinutes >= 9);
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
            var statut = new ExecControleDocumentStatut
            {
                Id = statutId,
                MachineCode = "MAS19",
                ExecControleOf = new ExecControleOf { Statut = StatutsExecution.EnCours }
            };
            _repository.Setup(r => r.GetDocumentStatutByIdAsync(statutId)).ReturnsAsync(statut);
            _repository.Setup(r => r.GetPeriodicitesPlanMachineAsync("MAS19")).ReturnsAsync(new List<string> { "démarrage" });

            await _service.MarquerDocumentTermineAsync(statutId, "M1", "demarrage");

            Assert.True(statut.EstDemarrageTermine);
            Assert.True(statut.EstTermine);
            Assert.Equal("M1", statut.MatriculeOperateur);
        }
    }
}
