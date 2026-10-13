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
    public class ExecutionOfServiceTests
    {
        private const string NumeroOf = "OF001";

        private readonly Mock<IOperateurRepository> _repository = new();
        private readonly Mock<IOccurrenceService> _occurrenceService = new();
        private readonly Mock<IOccurrenceRepository> _occurrenceRepository = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = UnitOfWorkMock.Transactionnelle();
        private readonly ExecutionOfService _service;

        private readonly Guid _execId = Guid.NewGuid();
        private readonly ExecControleOf _execOf;

        public ExecutionOfServiceTests()
        {
            _service = new ExecutionOfService(_repository.Object, _occurrenceService.Object, _occurrenceRepository.Object, _unitOfWork.Object);

            _execOf = new ExecControleOf { Id = _execId, NumeroOf = NumeroOf, TypeOf = TypesOf.Assemblage, Statut = StatutsExecution.EnCours };
            _repository.Setup(r => r.GetExecOfWithIntermediairesAsync(_execId)).ReturnsAsync(_execOf);
            _repository.Setup(r => r.GetExecOfByIdAsync(_execId)).ReturnsAsync(_execOf);
            _repository.Setup(r => r.GetDocumentStatutsAsync(_execId, It.IsAny<Guid?>())).ReturnsAsync(new List<ExecControleDocumentStatut>());
            _repository.Setup(r => r.GetSessionsOuvertesAsync(_execId)).ReturnsAsync(() => _sessionsOuvertes.ToList());
        }

        private readonly List<ExecControleOfSession> _sessionsOuvertes = new();

        private ExecControleOfSession SessionActive(string statut = StatutsSession.EnCours)
        {
            var session = new ExecControleOfSession { Id = Guid.NewGuid(), ExecControleOfId = _execId, Equipe = "E1", Statut = statut };
            _repository.Setup(r => r.GetActiveSessionAsync(_execId)).ReturnsAsync(session);
            _sessionsOuvertes.Add(session);
            return session;
        }

        private ExecControleOfSession AutreEquipe(string equipe, string statut)
        {
            var session = new ExecControleOfSession { Id = Guid.NewGuid(), ExecControleOfId = _execId, Equipe = equipe, Statut = statut, SuspendedAt = DateTime.Now.AddMinutes(-5) };
            _sessionsOuvertes.Add(session);
            return session;
        }

        // ───────────── Plusieurs équipes : une seule en production ─────────────

        [Fact]
        public async Task Reprise_RefuseeTantQuUneAutreEquipeProduit()
        {
            _execOf.Statut = StatutsExecution.EnCours;
            SessionActive(StatutsSession.EnCours).Equipe = "E2";
            AutreEquipe("E1", StatutsSession.EnPause);

            var ex = await Assert.ThrowsAsync<EtatExecutionInvalideException>(() => _service.ReprendreDepuisPauseAsync(_execId, "E1"));
            Assert.Contains("E2", ex.Message);
        }

        [Fact]
        public async Task Reprise_DeLEquipeDemandee_QuandPersonneNeProduit()
        {
            _execOf.Statut = StatutsExecution.EnPause;
            _execOf.SuspendedAt = DateTime.Now.AddMinutes(-3);
            var e2 = SessionActive(StatutsSession.EnPause);
            e2.Equipe = "E2";
            var e1 = AutreEquipe("E1", StatutsSession.EnPause);
            _unitOfWork.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<bool>>>())).Returns<Func<Task<bool>>>(f => f());

            Assert.True(await _service.ReprendreDepuisPauseAsync(_execId, "E1"));

            Assert.Equal((StatutsSession.EnCours, StatutsSession.EnPause, StatutsExecution.EnCours), (e1.Statut, e2.Statut, _execOf.Statut));
        }

        [Fact]
        public async Task FinDePoste_AutreEquipeEnPause_LOfResteEnPause()
        {
            SessionActive(StatutsSession.EnCours);
            AutreEquipe("E2", StatutsSession.EnPause);
            _unitOfWork.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<bool>>>())).Returns<Func<Task<bool>>>(f => f());

            Assert.True(await _service.TerminerPosteAsync(_execId));

            Assert.Equal(StatutsExecution.EnPause, _execOf.Statut);
        }

        [Fact]
        public async Task ClotureOf_TermineToutesLesEquipesOuvertes()
        {
            var e1 = SessionActive(StatutsSession.EnCours);
            var e2 = AutreEquipe("E2", StatutsSession.EnPause);
            _unitOfWork.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<bool>>>())).Returns<Func<Task<bool>>>(f => f());

            Assert.True(await _service.CloturerOfAsync(_execId));

            Assert.Equal((StatutsSession.Termine, StatutsSession.Termine), (e1.Statut, e2.Statut));
        }

        // ───────────── Démarrage : règles portées par l'opération ─────────────

        private ExecControleOf? _demarre;

        private void PreparerDemarrage(Operation operation, string? machinePlan = null)
        {
            _repository.Setup(r => r.GetOfAsync(NumeroOf)).ReturnsAsync(new MfgheadOrdreFabrication { NumeroOf = NumeroOf, CodeArticle = "ART1" });
            _repository.Setup(r => r.CanStartOperationAsync(NumeroOf, operation.Code)).ReturnsAsync(true);
            _repository.Setup(r => r.GetOperationAsync(operation.Code)).ReturnsAsync(operation);
            _repository.Setup(r => r.GetPlanActifAsync("ART1", operation.Code)).ReturnsAsync(new DocumentEntete { Id = Guid.NewGuid(), MachineCode = machinePlan });
            _repository.Setup(r => r.AddExecControleOf(It.IsAny<ExecControleOf>())).Callback<ExecControleOf>(e => _demarre = e);
            _repository.Setup(r => r.GetExecOfByIdAsync(It.IsAny<Guid>())).ReturnsAsync(() => _demarre);
        }

        [Fact]
        public async Task DemarrerOf_OperationAvecMachine_EnregistreLaMachineSansCotes()
        {
            PreparerDemarrage(new Operation { Code = "USINAG", Libelle = "Usinage", AvecMachine = true });

            await _service.DemarrerOfAsync(new DemarrerOfRequest { NumeroOf = NumeroOf, OperationCode = "USINAG", MachineCode = "TRF14", Longueur = 10 });

            Assert.Equal("TRF14", _demarre!.MachineCode);
            Assert.Equal("TRF14", _demarre.MachineCodePrevu);
            Assert.Null(_demarre.LongueurNominale);
        }

        [Fact]
        public async Task DemarrerOf_OperationAvecMachine_SansMachine_Refuse()
        {
            PreparerDemarrage(new Operation { Code = "ESTOMP", Libelle = "Estompage", AvecMachine = true });

            await Assert.ThrowsAsync<RegleMetierException>(() =>
                _service.DemarrerOfAsync(new DemarrerOfRequest { NumeroOf = NumeroOf, OperationCode = "ESTOMP" }));
            _repository.Verify(r => r.AddExecControleOf(It.IsAny<ExecControleOf>()), Times.Never);
        }

        [Fact]
        public async Task DemarrerOf_OperationAvecCotes_ConserveLesCotesEtLaMachineDuPlan()
        {
            PreparerDemarrage(new Operation { Code = "TRONC", Libelle = "Tronçonnage", AvecMachine = true, AvecCotesTronconnage = true }, machinePlan: "TRN26");

            await _service.DemarrerOfAsync(new DemarrerOfRequest { NumeroOf = NumeroOf, OperationCode = "TRONC", MachineCode = "TRN27", Longueur = 120.5, Diametre = 32 });

            Assert.Equal(120.5m, _demarre!.LongueurNominale);
            Assert.Equal(32m, _demarre.DiametreNominal);
            Assert.Equal("TRN26", _demarre.MachineCodePrevu);
            Assert.Equal("TRN27", _demarre.MachineCode);
        }

        // ───────────── Pause ─────────────

        [Fact]
        public async Task MettreEnPause_TypeInconnu_Refuse()
        {
            await Assert.ThrowsAsync<RegleMetierException>(() => _service.MettreEnPauseAsync(_execId, "raison", "SIESTE"));
            _repository.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task MettreEnPause_OfNonEnCours_RetourneFaux()
        {
            _execOf.Statut = StatutsExecution.EnPause;

            Assert.False(await _service.MettreEnPauseAsync(_execId, "raison", TypesPause.Dejeuner));
            _unitOfWork.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<bool>>>()), Times.Never);
        }

        [Fact]
        public async Task MettreEnPause_Valide_MetOfEtSessionEnPauseDansUneTransaction()
        {
            var session = SessionActive();

            Assert.True(await _service.MettreEnPauseAsync(_execId, "repas", TypesPause.Dejeuner));

            Assert.Equal(StatutsExecution.EnPause, _execOf.Statut);
            Assert.NotNull(_execOf.SuspendedAt);
            Assert.Equal(StatutsSession.EnPause, session.Statut);
            _occurrenceService.Verify(o => o.AjouterRemarqueTrancheEnCoursAsync(_execId, It.Is<string>(s => s.Contains(TypesPause.Dejeuner) && s.Contains("repas"))), Times.Once);
            _unitOfWork.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<bool>>>()), Times.Once);
        }

        // ───────────── Reprise ─────────────

        [Fact]
        public async Task ReprendreDepuisPause_CumuleLeTempsDePause()
        {
            _execOf.Statut = StatutsExecution.EnPause;
            _execOf.SuspendedAt = DateTime.Now.AddMinutes(-30);
            var session = SessionActive(StatutsSession.EnPause);
            session.SuspendedAt = DateTime.Now.AddMinutes(-30);

            Assert.True(await _service.ReprendreDepuisPauseAsync(_execId));

            Assert.Equal(StatutsExecution.EnCours, _execOf.Statut);
            Assert.Null(_execOf.SuspendedAt);
            Assert.Null(session.SuspendedAt);
            Assert.InRange(_execOf.TempsPauseTotalMinutes, 29.5, 31);
            Assert.Equal(StatutsSession.EnCours, session.Statut);
            Assert.InRange(session.TempsPauseTotalMinutes, 29.5, 31);
        }

        [Fact]
        public async Task ReprendreDepuisPause_OfNonEnPause_RetourneFaux()
        {
            Assert.False(await _service.ReprendreDepuisPauseAsync(_execId));
        }

        // ───────────── Fin de poste ─────────────

        [Fact]
        public async Task TerminerPoste_SansSessionActive_RetourneFaux()
        {
            Assert.False(await _service.TerminerPosteAsync(_execId));
            _repository.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task TerminerPoste_TermineLaSessionSansCloturerLOf()
        {
            var session = SessionActive();

            Assert.True(await _service.TerminerPosteAsync(_execId));

            Assert.Equal(StatutsSession.Termine, session.Statut);
            Assert.NotNull(session.DateFin);
            Assert.Equal(StatutsExecution.EnCours, _execOf.Statut);
        }

        // ───────────── Clôture ─────────────

        [Fact]
        public async Task CloturerOf_DejaCloture_RetourneFaux()
        {
            _execOf.Statut = StatutsExecution.Cloture;

            Assert.False(await _service.CloturerOfAsync(_execId));
            _unitOfWork.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<bool>>>()), Times.Never);
        }

        [Fact]
        public async Task CloturerOf_SupprimeLesOccurrencesNonReponduesEtTermineLesDocuments()
        {
            var repondue = new ExecPrelevementIntermediaire { Id = Guid.NewGuid(), TrancheHoraire = "08:00", EstRepondu = true };
            var nonRepondue = new ExecPrelevementIntermediaire { Id = Guid.NewGuid(), TrancheHoraire = "09:00", EstRepondu = false };
            _execOf.ExecPrelevementIntermediaires.Add(repondue);
            _execOf.ExecPrelevementIntermediaires.Add(nonRepondue);
            var document = new ExecControleDocumentStatut { Id = Guid.NewGuid(), EstTermine = false };
            _repository.Setup(r => r.GetDocumentStatutsAsync(_execId, It.IsAny<Guid?>())).ReturnsAsync(new List<ExecControleDocumentStatut> { document });
            var session = SessionActive();
            IEnumerable<ExecPrelevementIntermediaire>? supprimees = null;
            _occurrenceRepository.Setup(r => r.RemoveIntermediaires(It.IsAny<IEnumerable<ExecPrelevementIntermediaire>>()))
                .Callback<IEnumerable<ExecPrelevementIntermediaire>>(o => supprimees = o.ToList());

            Assert.True(await _service.CloturerOfAsync(_execId));

            Assert.Equal(StatutsExecution.Cloture, _execOf.Statut);
            Assert.Equal(StatutsSession.Termine, session.Statut);
            Assert.True(document.EstTermine);
            Assert.Equal(nonRepondue.Id, Assert.Single(supprimees!).Id);
        }

    }
}
