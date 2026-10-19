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
        private readonly Mock<IDemarragePosteRepository> _postes = new();
        private readonly ExecutionOfService _service;

        private readonly Guid _execId = Guid.NewGuid();
        private readonly ExecControleOf _execOf;
        private readonly List<ExecControleOfSession> _sessionsOuvertes = new();
        private readonly List<ExecSessionPost> _postesTenus = new();

        public ExecutionOfServiceTests()
        {
            _service = new ExecutionOfService(_repository.Object, _occurrenceService.Object, _occurrenceRepository.Object, _unitOfWork.Object, _postes.Object);

            _execOf = new ExecControleOf { Id = _execId, NumeroOf = NumeroOf, TypeOf = TypesOf.Assemblage, Statut = StatutsExecution.EnCours };
            _repository.Setup(r => r.GetExecOfWithIntermediairesAsync(_execId)).ReturnsAsync(_execOf);
            _repository.Setup(r => r.GetExecOfByIdAsync(_execId)).ReturnsAsync(_execOf);
            _repository.Setup(r => r.GetStatutsOfAsync(_execId)).ReturnsAsync(new List<ExecControleDocumentStatut>());
            _postes.Setup(p => p.GetSessionsOuvertesAsync(_execId)).ReturnsAsync(() => _sessionsOuvertes.ToList());
            _postes.Setup(p => p.GetPostesEnCoursAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid sessionId) => _postesTenus.Where(p => p.SessionId == sessionId && p.EndedAt == null).ToList());
        }

        private ExecControleOfSession Equipe(string equipe, string statut = StatutsSession.EnCours, params string[] postes)
        {
            var session = new ExecControleOfSession
            {
                Id = Guid.NewGuid(), ExecControleOfId = _execId, Equipe = equipe, Statut = statut,
                SuspendedAt = statut == StatutsSession.EnPause ? DateTime.Now.AddMinutes(-5) : null
            };
            _sessionsOuvertes.Add(session);
            foreach (var poste in postes)
                _postesTenus.Add(new ExecSessionPost
                {
                    Id = Guid.NewGuid(), SessionId = session.Id, ExecControleOfId = _execId, PosteCode = poste, StartedAt = DateTime.Now.AddHours(-1)
                });
            return session;
        }

        private ExecSessionPost Poste(ExecControleOfSession session, string posteCode) =>
            _postesTenus.Single(p => p.SessionId == session.Id && p.PosteCode == posteCode);

        // ───────────── Pause et reprise : par équipe ─────────────

        [Fact]
        public async Task Reprise_DUneEquipeSansSessionOuverte_Refusee()
        {
            _execOf.Statut = StatutsExecution.EnPause;
            Equipe("E1", StatutsSession.EnPause, "PAS71");

            var ex = await Assert.ThrowsAsync<EtatExecutionInvalideException>(() => _service.ReprendreDepuisPauseAsync(_execId, "E2"));
            Assert.Contains("E2", ex.Message);
        }

        [Fact]
        public async Task Reprise_DeLEquipeEnPause_LOfRepasseEnCours()
        {
            _execOf.Statut = StatutsExecution.EnPause;
            _execOf.SuspendedAt = DateTime.Now.AddMinutes(-30);
            var e1 = Equipe("E1", StatutsSession.EnPause, "PAS71");
            e1.SuspendedAt = DateTime.Now.AddMinutes(-30);

            Assert.True(await _service.ReprendreDepuisPauseAsync(_execId, "E1"));

            Assert.Equal((StatutsSession.EnCours, StatutsExecution.EnCours), (e1.Statut, _execOf.Statut));
            Assert.Null(e1.SuspendedAt);
            Assert.InRange(e1.TempsPauseTotalMinutes, 29.5, 31);
            Assert.InRange(_execOf.TempsPauseTotalMinutes, 29.5, 31);
        }

        [Fact]
        public async Task Reprise_EquipeDejaEnProduction_RetourneFaux()
        {
            Equipe("E1", StatutsSession.EnCours, "PAS71");

            Assert.False(await _service.ReprendreDepuisPauseAsync(_execId, "E1"));
        }

        [Fact]
        public async Task Pause_DUneEquipe_LesAutresEquipesContinuent()
        {
            var e1 = Equipe("E1", StatutsSession.EnCours, "PAS72");
            var e2 = Equipe("E2", StatutsSession.EnCours, "PAS71");

            Assert.True(await _service.MettreEnPauseAsync(_execId, "repas", TypesPause.Dejeuner, "E1"));

            Assert.Equal((StatutsSession.EnPause, StatutsSession.EnCours), (e1.Statut, e2.Statut));
            Assert.True(e1.HadLunchBreak);
            Assert.Equal(StatutsExecution.EnCours, _execOf.Statut);
        }

        [Fact]
        public async Task Pause_DeToutesLesEquipes_MetLOfEnPause()
        {
            Equipe("E1", StatutsSession.EnPause, "PAS72");
            var e2 = Equipe("E2", StatutsSession.EnCours, "PAS71");

            Assert.True(await _service.MettreEnPauseAsync(_execId, "repas", TypesPause.Dejeuner, "E2"));

            Assert.Equal(StatutsSession.EnPause, e2.Statut);
            Assert.Equal(StatutsExecution.EnPause, _execOf.Statut);
            Assert.NotNull(_execOf.SuspendedAt);
        }

        [Fact]
        public async Task Pause_PlusieursEquipesSansPreciserLaSienne_Refusee()
        {
            Equipe("E1", StatutsSession.EnCours, "PAS72");
            Equipe("E2", StatutsSession.EnCours, "PAS71");

            await Assert.ThrowsAsync<RegleMetierException>(() => _service.MettreEnPauseAsync(_execId, "repas", TypesPause.Dejeuner));
        }

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
        public async Task MettreEnPause_SeuleEquipe_TraceLaPauseDansUneTransaction()
        {
            var session = Equipe("E1", StatutsSession.EnCours, "PAS71");

            Assert.True(await _service.MettreEnPauseAsync(_execId, "repas", TypesPause.Dejeuner));

            Assert.Equal((StatutsSession.EnPause, StatutsExecution.EnPause), (session.Statut, _execOf.Statut));
            _occurrenceService.Verify(o => o.AjouterRemarqueTrancheEnCoursAsync(_execId,
                It.Is<string>(s => s.Contains(TypesPause.Dejeuner) && s.Contains("repas") && s.Contains("E1"))), Times.Once);
            _unitOfWork.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<bool>>>()), Times.Once);
        }

        // ───────────── Fin de poste : poste par poste ─────────────

        [Fact]
        public async Task FinDePoste_DUnPoste_LesAutresPostesDeLEquipeContinuent()
        {
            var e1 = Equipe("E1", StatutsSession.EnCours, "PAS71", "PAS72");

            Assert.True(await _service.TerminerPostesAsync(_execId, e1.Id, new[] { "PAS71" }, "55555"));

            Assert.NotNull(Poste(e1, "PAS71").EndedAt);
            Assert.Equal("55555", Poste(e1, "PAS71").EndedBy);
            Assert.Null(Poste(e1, "PAS72").EndedAt);
            Assert.Equal(StatutsSession.EnCours, e1.Statut);
            _occurrenceService.Verify(o => o.AjouterRemarqueTrancheEnCoursAsync(_execId, It.Is<string>(s => s.Contains("PAS71") && s.Contains("E1"))), Times.Once);
        }

        [Fact]
        public async Task FinDePoste_DuDernierPoste_TermineLaSessionEtLaisseLOfEnCours()
        {
            var e1 = Equipe("E1", StatutsSession.EnCours, "PAS72");
            e1.ClosingRequest = null;
            var etats = new List<(bool PosteTermine, string Session)>();
            _repository.Setup(r => r.SaveChangesAsync()).Callback(() => etats.Add((Poste(e1, "PAS72").EndedAt != null, e1.Statut))).Returns(Task.CompletedTask);

            Assert.True(await _service.TerminerPostesAsync(_execId, e1.Id, new[] { "PAS72" }, "55555"));

            Assert.Equal((StatutsSession.Termine, StatutsExecution.EnCours), (e1.Statut, _execOf.Statut));
            Assert.NotNull(e1.DateFin);
            // Le poste est enregistré terminé avant la session (une session terminée ne tient plus aucun poste).
            Assert.Equal((true, StatutsSession.EnCours), etats[0]);
        }

        [Fact]
        public async Task FinDePoste_PosteNonTenuParLEquipe_RetourneFaux()
        {
            var e1 = Equipe("E1", StatutsSession.EnCours, "PAS72");

            Assert.False(await _service.TerminerPostesAsync(_execId, e1.Id, new[] { "PAS71" }, "55555"));
            _repository.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task FinDePoste_SessionInconnue_RetourneFaux()
        {
            Equipe("E1", StatutsSession.EnCours, "PAS72");

            Assert.False(await _service.TerminerPostesAsync(_execId, Guid.NewGuid(), new[] { "PAS72" }, "55555"));
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

        // ───────────── Clôture ─────────────

        [Fact]
        public async Task CloturerOf_DejaCloture_RetourneFaux()
        {
            _execOf.Statut = StatutsExecution.Cloture;

            Assert.False(await _service.CloturerOfAsync(_execId));
            _unitOfWork.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<bool>>>()), Times.Never);
        }

        [Fact]
        public async Task CloturerOf_TermineLesPostesPuisLesSessionsDeToutesLesEquipes()
        {
            var e1 = Equipe("E1", StatutsSession.EnCours, "PAS72");
            var e2 = Equipe("E2", StatutsSession.EnPause, "PAS71");
            var premiereSauvegarde = ((bool, bool, string, string)?)null;
            _repository.Setup(r => r.SaveChangesAsync()).Callback(() => premiereSauvegarde ??=
                (Poste(e1, "PAS72").EndedAt != null, Poste(e2, "PAS71").EndedAt != null, e1.Statut, e2.Statut)).Returns(Task.CompletedTask);

            Assert.True(await _service.CloturerOfAsync(_execId));

            Assert.Equal((StatutsSession.Termine, StatutsSession.Termine), (e1.Statut, e2.Statut));
            Assert.Equal((true, true, StatutsSession.EnCours, StatutsSession.EnPause), premiereSauvegarde);
        }

        [Fact]
        public async Task CloturerOf_SupprimeLesOccurrencesNonReponduesEtTermineLesDocuments()
        {
            var repondue = new ExecPrelevementIntermediaire { Id = Guid.NewGuid(), TrancheHoraire = "08:00", EstRepondu = true };
            var nonRepondue = new ExecPrelevementIntermediaire { Id = Guid.NewGuid(), TrancheHoraire = "09:00", EstRepondu = false };
            _execOf.ExecPrelevementIntermediaires.Add(repondue);
            _execOf.ExecPrelevementIntermediaires.Add(nonRepondue);
            var document = new ExecControleDocumentStatut { Id = Guid.NewGuid(), EstTermine = false };
            var verification = new ExecControleDocumentStatut { Id = Guid.NewGuid(), TypeDocument = TypesDocumentExecution.VerifMachine, EstTermine = false };
            _repository.Setup(r => r.GetStatutsOfAsync(_execId)).ReturnsAsync(new List<ExecControleDocumentStatut> { document, verification });
            var session = Equipe("E1", StatutsSession.EnCours, "PAS71");
            IEnumerable<ExecPrelevementIntermediaire>? supprimees = null;
            _occurrenceRepository.Setup(r => r.RemoveIntermediaires(It.IsAny<IEnumerable<ExecPrelevementIntermediaire>>()))
                .Callback<IEnumerable<ExecPrelevementIntermediaire>>(o => supprimees = o.ToList());

            Assert.True(await _service.CloturerOfAsync(_execId));

            Assert.Equal(StatutsExecution.Cloture, _execOf.Statut);
            Assert.Equal(StatutsSession.Termine, session.Statut);
            Assert.True(document.EstTermine);
            // Une vérification machine garde son état réel (jamais cochée sans saisie).
            Assert.False(verification.EstTermine);
            Assert.Equal(nonRepondue.Id, Assert.Single(supprimees!).Id);
        }
    }
}
