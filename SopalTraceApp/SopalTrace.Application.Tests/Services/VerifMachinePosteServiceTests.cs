using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using SopalTrace.Application.Dtos.VerifMachine;
using SopalTrace.Application.DTOs.Execution.Operateur;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Application.Services.Execution;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;

namespace SopalTrace.Application.Tests.Services
{
    /// <summary>Vérifications machine d'un poste : un moment n'est validé que si tous ses points sont répondus.</summary>
    public class VerifMachinePosteServiceTests
    {
        private const string Poste = "PAS72";
        private readonly Guid _execId = Guid.NewGuid();
        private readonly Guid _planBee46 = Guid.NewGuid();
        private readonly Guid _demarrage1 = Guid.NewGuid();
        private readonly Guid _demarrage2 = Guid.NewGuid();
        private readonly Guid _fin = Guid.NewGuid();
        private readonly ExecControleOfSession _session;
        private readonly ExecControleDocumentStatut _statut;
        private readonly List<ExecVerifMachineReponse> _reponses = new();
        private readonly Mock<IOperateurRepository> _repo = new();
        private readonly VerifMachinePosteService _service;

        public VerifMachinePosteServiceTests()
        {
            var execOf = new ExecControleOf { Id = _execId, NumeroOf = "OF1", Statut = StatutsExecution.EnCours };
            _session = new ExecControleOfSession { Id = Guid.NewGuid(), Equipe = "E1", Statut = StatutsSession.EnCours };
            _statut = new ExecControleDocumentStatut
            {
                Id = Guid.NewGuid(), ExecControleOfId = _execId, ExecControleOf = execOf, PosteCode = Poste, MachineCode = "BEE46",
                DocId = _planBee46, SessionId = _session.Id, TypeDocument = TypesDocumentExecution.VerifMachine, DateExecution = DateTime.Today
            };

            _repo.Setup(r => r.GetExecOfByIdAsync(_execId)).ReturnsAsync(execOf);
            _repo.Setup(r => r.GetPostesForExecutionAsync(_execId)).ReturnsAsync(new[] { Poste });
            _repo.Setup(r => r.GetDocumentStatutsAsync(_execId, _session.Id)).ReturnsAsync(new[] { _statut });
            _repo.Setup(r => r.GetDocumentStatutByIdAsync(_statut.Id)).ReturnsAsync(_statut);
            _repo.Setup(r => r.GetPosteWithMachinesAsync(Poste)).ReturnsAsync(new PosteTravail
            {
                CodePoste = Poste,
                CodeMachines = { new Machine { CodeMachine = "BEE46", Actif = true }, new Machine { CodeMachine = "MAS26", Actif = true }, new Machine { CodeMachine = "MAS99", Actif = true } }
            });
            _repo.Setup(r => r.GetMachineLabelsAsync(It.IsAny<IEnumerable<string>>())).ReturnsAsync(new Dictionary<string, string>());
            _repo.Setup(r => r.GetPlanVerifMachinePourOfAsync(It.IsAny<Guid>(), "MAS26")).ReturnsAsync(Guid.NewGuid());
            _repo.Setup(r => r.GetPlanVerifMachinePourOfAsync(It.IsAny<Guid>(), "MAS99")).ReturnsAsync((Guid?)null);
            _repo.Setup(r => r.GetEcheancesPlanVerifMachineAsync(It.Is<Guid>(g => g != _planBee46))).ReturnsAsync(new List<(Guid, string)>
            {
                (Guid.NewGuid(), PeriodicitesControle.FinPoste)
            });
            _repo.Setup(r => r.GetEcheancesPlanVerifMachineAsync(_planBee46)).ReturnsAsync(new List<(Guid, string)>
            {
                (_demarrage1, PeriodicitesControle.Demarrage), (_demarrage2, PeriodicitesControle.Demarrage), (_fin, PeriodicitesControle.FinPoste)
            });
            _repo.Setup(r => r.GetExecVerifMachineReponsesAsync(_statut.Id)).ReturnsAsync(() => _reponses.ToList());
            _repo.Setup(r => r.AddExecVerifMachineReponses(It.IsAny<IEnumerable<ExecVerifMachineReponse>>()))
                .Callback<IEnumerable<ExecVerifMachineReponse>>(r => _reponses.AddRange(r));
            _repo.Setup(r => r.GetPeriodicitesPlanMachineAsync("BEE46")).ReturnsAsync(new List<string> { PeriodicitesControle.Demarrage, PeriodicitesControle.FinPoste });

            var demarrages = new Mock<IDemarragePosteRepository>();
            demarrages.Setup(d => d.GetSessionAsync(_execId, Poste, null)).ReturnsAsync(_session);
            demarrages.Setup(d => d.EstEnCoursAsync(_session.Id, Poste)).ReturnsAsync(true);
            _service = new VerifMachinePosteService(_repo.Object, Mock.Of<IAssemblageExecutionService>(), demarrages.Object);
        }

        private static ExecVerifMachineReponseDto Reponse(Guid point, bool? conforme, string? observation = null) =>
            new() { DocumentVerifMachineEcheanceId = point, Conforme = conforme, Observation = observation };

        private ValiderVerifMachineRequest Demarrage(params ExecVerifMachineReponseDto[] reponses) =>
            new() { Periodicite = PeriodicitesControle.Demarrage, Reponses = reponses.ToList() };

        [Fact]
        public async Task Machines_EtatParMoment()
        {
            var dto = await _service.GetMachinesAsync(_execId, Poste, PeriodicitesControle.Demarrage);

            var etats = dto.Machines.ToDictionary(m => m.MachineCode, m => m.Etat);
            Assert.Equal(EtatsVerifMachine.AFaire, etats["BEE46"]);
            Assert.Equal(EtatsVerifMachine.NonPrevue, etats["MAS26"]);
            Assert.Equal(EtatsVerifMachine.PlanManquant, etats["MAS99"]);
            Assert.Equal((1, 0), (dto.Attendues, dto.Validees));
        }

        [Fact]
        public async Task Validation_RefuseeTantQuUnPointDuMomentManque()
        {
            var erreur = await Assert.ThrowsAsync<RegleMetierException>(() => _service.ValiderAsync(_statut.Id, Demarrage(Reponse(_demarrage1, true)), "M1"));

            Assert.Contains("1 point", erreur.Message);
            Assert.False(_statut.EstDemarrageTermine);
            _repo.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Validation_NonConformeSansObservation_Refusee()
        {
            await Assert.ThrowsAsync<RegleMetierException>(() =>
                _service.ValiderAsync(_statut.Id, Demarrage(Reponse(_demarrage1, true), Reponse(_demarrage2, false)), "M1"));
        }

        [Fact]
        public async Task Validation_TousLesPointsDuMoment_ValideLeDemarrageSeulement()
        {
            await _service.ValiderAsync(_statut.Id, Demarrage(Reponse(_demarrage1, true), Reponse(_demarrage2, false, "Fuite")), "M1");

            Assert.True(_statut.EstDemarrageTermine);
            // La machine a aussi une vérification de fin de poste : le document reste ouvert.
            Assert.False(_statut.EstTermine);
            Assert.Equal(2, _reponses.Count);
            var etat = (await _service.GetMachinesAsync(_execId, Poste, PeriodicitesControle.Demarrage)).Machines.Single(m => m.MachineCode == "BEE46");
            Assert.Equal((EtatsVerifMachine.Validee, 2, 2), (etat.Etat, etat.PointsRepondus, etat.PointsTotal));
        }

        [Fact]
        public async Task Validation_ReponsesDejaEnregistrees_Suffisent()
        {
            _reponses.Add(new ExecVerifMachineReponse { DocumentVerifMachineEcheanceId = _demarrage1, Conforme = true, DateExecution = DateTime.Now });
            _reponses.Add(new ExecVerifMachineReponse { DocumentVerifMachineEcheanceId = _demarrage2, Conforme = true, DateExecution = DateTime.Now });

            await _service.ValiderAsync(_statut.Id, Demarrage(), "M1");

            Assert.True(_statut.EstDemarrageTermine);
        }

        [Fact]
        public async Task Correction_RouvreLeMoment()
        {
            _statut.EstDemarrageTermine = true;

            await _service.CorrigerAsync(_statut.Id, PeriodicitesControle.Demarrage, "M1");

            Assert.False(_statut.EstDemarrageTermine);
        }

        [Fact]
        public async Task SessionEnPauseOuTerminee_Refusee()
        {
            _session.Statut = StatutsSession.EnPause;
            await Assert.ThrowsAsync<EtatExecutionInvalideException>(() => _service.CorrigerAsync(_statut.Id, PeriodicitesControle.Demarrage, "M1"));

            _session.Statut = StatutsSession.EnCours;
            _statut.SessionId = Guid.NewGuid();
            await Assert.ThrowsAsync<EtatExecutionInvalideException>(() => _service.CorrigerAsync(_statut.Id, PeriodicitesControle.Demarrage, "M1"));
        }

        [Fact]
        public async Task Historique_FiltreParMachineEquipeEtJour()
        {
            _repo.Setup(r => r.GetStatutsVerifMachineAsync(_execId)).ReturnsAsync(new List<ExecControleDocumentStatut>
            {
                _statut,
                new() { Id = Guid.NewGuid(), PosteCode = Poste, MachineCode = "BEE46", Equipe = "E2", DateExecution = DateTime.Today.AddDays(-1) },
                new() { Id = Guid.NewGuid(), PosteCode = Poste, MachineCode = "MAS26", Equipe = "E1", DateExecution = DateTime.Today }
            });
            _statut.Equipe = "E1";

            var liste = await _service.GetHistoriqueAsync(_execId, Poste, "BEE46", "E1", DateTime.Today);

            var seul = Assert.Single(liste);
            Assert.Equal(_statut.Id, seul.StatutId);
            Assert.True(seul.SessionActive);
        }
    }
}
