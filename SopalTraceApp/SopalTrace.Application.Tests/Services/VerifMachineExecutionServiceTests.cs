using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using SopalTrace.Application.Dtos.VerifMachine;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Application.Services.Execution;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;

namespace SopalTrace.Application.Tests.Services
{
    public class VerifMachineExecutionServiceTests
    {
        private readonly Mock<IOperateurRepository> _repository = new();
        private readonly VerifMachineExecutionService _service;

        private readonly Guid _execId = Guid.NewGuid();
        private readonly ExecControleOf _execOf;

        public VerifMachineExecutionServiceTests()
        {
            _service = new VerifMachineExecutionService(_repository.Object);
            _execOf = new ExecControleOf { Id = _execId, NumeroOf = "OF001", Statut = StatutsExecution.EnCours };
            _repository.Setup(r => r.GetExecOfByIdAsync(_execId)).ReturnsAsync(_execOf);
            _repository.Setup(r => r.GetPostesForExecutionAsync(_execId)).ReturnsAsync(new List<string> { "PAS71" });
            _repository.Setup(r => r.GetExecVerifMachineReponsesAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<ExecVerifMachineReponse>());
        }

        private ExecControleDocumentStatut StatutVerifMachine(Guid? sessionId = null)
        {
            var statut = new ExecControleDocumentStatut
            {
                Id = Guid.NewGuid(),
                ExecControleOfId = _execId,
                ExecControleOf = _execOf,
                TypeDocument = TypesDocumentExecution.VerifMachine,
                MachineCode = "MAS19",
                PosteCode = "PAS71",
                SessionId = sessionId
            };
            _repository.Setup(r => r.GetDocumentStatutByIdAsync(statut.Id)).ReturnsAsync(statut);
            return statut;
        }

        private static SaveExecVerifMachineRequest Demande(Guid statutId) => new()
        {
            ExecControleDocumentStatutId = statutId,
            MatriculeOperateur = "M1",
            Reponses = new List<ExecVerifMachineReponseDto>
            {
                new() { DocumentVerifMachineEcheanceId = Guid.NewGuid(), Conforme = true }
            }
        };

        [Fact]
        public async Task GetExecVerifMachine_DocumentDUnAutreType_Introuvable()
        {
            var statut = StatutVerifMachine();
            statut.TypeDocument = TypesDocumentExecution.PlanAssemblage;

            await Assert.ThrowsAsync<RessourceIntrouvableException>(() => _service.GetExecVerifMachineAsync(statut.Id));
        }

        [Fact]
        public async Task SaveExecVerifMachine_OfCloture_Refuse()
        {
            var statut = StatutVerifMachine();
            _execOf.Statut = StatutsExecution.Cloture;

            await Assert.ThrowsAsync<EtatExecutionInvalideException>(() => _service.SaveExecVerifMachineAsync(Demande(statut.Id)));
            _repository.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task SaveExecVerifMachine_SessionTerminee_Refuse()
        {
            var statut = StatutVerifMachine(sessionId: Guid.NewGuid());
            _repository.Setup(r => r.GetActiveSessionAsync(_execId)).ReturnsAsync(new ExecControleOfSession { Id = Guid.NewGuid(), Statut = StatutsSession.EnCours });

            await Assert.ThrowsAsync<EtatExecutionInvalideException>(() => _service.SaveExecVerifMachineAsync(Demande(statut.Id)));
            _repository.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task SaveExecVerifMachine_Valide_EnregistreLesReponsesAvecLeMatricule()
        {
            var statut = StatutVerifMachine();
            List<ExecVerifMachineReponse>? ajoutees = null;
            _repository.Setup(r => r.AddExecVerifMachineReponses(It.IsAny<IEnumerable<ExecVerifMachineReponse>>()))
                .Callback<IEnumerable<ExecVerifMachineReponse>>(r => ajoutees = r.ToList());

            await _service.SaveExecVerifMachineAsync(Demande(statut.Id));

            var reponse = Assert.Single(ajoutees!);
            Assert.Equal("M1", reponse.MatriculeOperateur);
            Assert.Equal(statut.Id, reponse.ExecControleDocumentStatutId);
            Assert.NotNull(statut.DateExecution);
            _repository.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task SaveExecVerifMachine_ConserveLHistorique_EtLaLectureRenvoieLaDerniereSaisie()
        {
            var statut = StatutVerifMachine();
            var echeance = Guid.NewGuid();
            _repository.Setup(r => r.GetExecVerifMachineReponsesAsync(statut.Id)).ReturnsAsync(new List<ExecVerifMachineReponse>
            {
                new() { DocumentVerifMachineEcheanceId = echeance, DateExecution = DateTime.Now.AddMinutes(-10), Conforme = false, MatriculeOperateur = "M1" },
                new() { DocumentVerifMachineEcheanceId = echeance, DateExecution = DateTime.Now, Conforme = true, MatriculeOperateur = "M2" }
            });
            _repository.Setup(r => r.GetDocumentStatutsForMachineAsync(_execId, "MAS19")).ReturnsAsync(new List<ExecControleDocumentStatut>());

            await _service.SaveExecVerifMachineAsync(Demande(statut.Id));
            var session = await _service.GetExecVerifMachineAsync(statut.Id);

            var reponse = Assert.Single(session.Reponses);
            Assert.True(reponse.Conforme);
            Assert.Equal("M2", reponse.MatriculeOperateur);
        }

        [Fact]
        public async Task CloturerTous_PosteNonAffecte_Refuse()
        {
            await Assert.ThrowsAsync<RegleMetierException>(() => _service.CloturerTousVerifMachineAsync(_execId, "PAS99"));
        }

        [Fact]
        public async Task CloturerTous_TermineLesVerificationsEtAjouteLeMarqueur()
        {
            var verif = new ExecControleDocumentStatut { Id = Guid.NewGuid(), PosteCode = "PAS71", TypeDocument = TypesDocumentExecution.VerifMachine };
            _repository.Setup(r => r.GetDocumentStatutsAsync(_execId, It.IsAny<Guid?>())).ReturnsAsync(new List<ExecControleDocumentStatut> { verif });
            ExecControleDocumentStatut? marqueur = null;
            _repository.Setup(r => r.AddExecControleDocumentStatut(It.IsAny<ExecControleDocumentStatut>()))
                .Callback<ExecControleDocumentStatut>(s => marqueur = s);

            await _service.CloturerTousVerifMachineAsync(_execId, "PAS71");

            Assert.True(verif.EstTermine);
            Assert.True(verif.EstDemarrageTermine);
            Assert.NotNull(marqueur);
            Assert.Equal(TypesDocumentExecution.ClotureVerifMachinePoste, marqueur!.TypeDocument);
        }

        [Fact]
        public async Task CloturerTous_MarqueurDejaPresent_NeLeRecreePas()
        {
            var marqueurExistant = new ExecControleDocumentStatut { Id = Guid.NewGuid(), PosteCode = "PAS71", TypeDocument = TypesDocumentExecution.ClotureVerifMachinePoste, EstTermine = true };
            _repository.Setup(r => r.GetDocumentStatutsAsync(_execId, It.IsAny<Guid?>())).ReturnsAsync(new List<ExecControleDocumentStatut> { marqueurExistant });

            await _service.CloturerTousVerifMachineAsync(_execId, "PAS71");

            _repository.Verify(r => r.AddExecControleDocumentStatut(It.IsAny<ExecControleDocumentStatut>()), Times.Never);
        }
    }
}
