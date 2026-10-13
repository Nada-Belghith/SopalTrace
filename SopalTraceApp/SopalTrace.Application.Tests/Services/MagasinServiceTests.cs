using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Services;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;
using Xunit;

namespace SopalTrace.Application.Tests.Services
{
    public class MagasinServiceTests
    {
        private const string NumeroOf = "OF001";
        private const string Magasinier = "33333";

        private readonly Mock<IMagasinRepository> _repository = new();
        private readonly Mock<ICurrentUserService> _currentUser = new();
        private readonly MagasinService _service;
        private readonly MfgheadOrdreFabrication _of;
        private readonly MagPreparationOf _preparation;

        public MagasinServiceTests()
        {
            _service = new MagasinService(_repository.Object, _currentUser.Object);
            _currentUser.Setup(c => c.MatriculeRequis).Returns(Magasinier);

            _of = new MfgheadOrdreFabrication
            {
                NumeroOf = NumeroOf, CodeArticle = "PF1", QuantitePrevue = 100,
                MfgmatBesoinOfs = new List<MfgmatBesoinOf>
                {
                    new() { NumeroOf = NumeroOf, CodeArticle = "CORPS-1", QuantiteRequise = 100 },
                    new() { NumeroOf = NumeroOf, CodeArticle = "JOINT-1", QuantiteRequise = 200 }
                }
            };
            _preparation = new MagPreparationOf { Id = Guid.NewGuid(), NumeroOf = NumeroOf, Statut = StatutsPreparationMagasin.EnCours, MatriculeMagasinier = Magasinier };
            _repository.Setup(r => r.GetOfAvecNomenclatureAsync(NumeroOf)).ReturnsAsync(_of);
            _repository.Setup(r => r.GetPreparationAsync(_preparation.Id)).ReturnsAsync(_preparation);
        }

        // ───────────── Préparation ─────────────

        [Fact]
        public async Task Ouvrir_SansPreparation_CreeAvecLeMagasinierDuJeton()
        {
            MagPreparationOf? creee = null;
            _repository.Setup(r => r.AjouterPreparation(It.IsAny<MagPreparationOf>())).Callback<MagPreparationOf>(p => creee = p);

            var dto = await _service.OuvrirPreparationAsync(NumeroOf);

            Assert.Equal((Magasinier, StatutsPreparationMagasin.EnCours), (creee!.MatriculeMagasinier, creee.Statut));
            Assert.Equal(2, dto.Composants.Count);
        }

        [Fact]
        public async Task Ouvrir_PreparationExistante_LaReprend()
        {
            _repository.Setup(r => r.GetPreparationActiveAsync(NumeroOf)).ReturnsAsync(_preparation);

            var dto = await _service.OuvrirPreparationAsync(NumeroOf);

            Assert.Equal(_preparation.Id, dto.Id);
            _repository.Verify(r => r.AjouterPreparation(It.IsAny<MagPreparationOf>()), Times.Never);
        }

        [Fact]
        public async Task Scan_ArticleDeLaNomenclature_EnregistreLotQuantiteEtRapport()
        {
            var resultat = await _service.ScannerComposantAsync(_preparation.Id, "cd:corps-1/lot:L1/rpt:QC1/qty:40");

            var lot = Assert.Single(_preparation.MagPreparationOfLots);
            Assert.Equal(("CORPS-1", "L1", "QC1", 40m), (lot.CodeArticle, lot.NumeroLotScanne, lot.QualityControlReport, lot.Quantite));
            Assert.Equal("cd:corps-1/lot:L1/rpt:QC1/qty:40", lot.ScannedContent);
            Assert.Equal(1, resultat.Preparation.ComposantsServis);
            Assert.Equal(40m, resultat.Preparation.Composants.First(c => c.CodeArticle == "CORPS-1").QuantiteServie);
        }

        [Fact]
        public async Task Scan_ArticleHorsNomenclature_Refuse()
        {
            await Assert.ThrowsAsync<RegleMetierException>(() => _service.ScannerComposantAsync(_preparation.Id, "VIS-9;L1"));
            Assert.Empty(_preparation.MagPreparationOfLots);
        }

        [Fact]
        public async Task Scan_MemeLotDeuxFois_409()
        {
            await _service.ScannerComposantAsync(_preparation.Id, "CORPS-1;L1");

            await Assert.ThrowsAsync<ScanEnDoubleException>(() => _service.ScannerComposantAsync(_preparation.Id, "CORPS-1;l1"));
            Assert.Single(_preparation.MagPreparationOfLots);
        }

        [Fact]
        public async Task Scan_PreparationTerminee_Refuse()
        {
            _preparation.Statut = StatutsPreparationMagasin.Termine;
            await Assert.ThrowsAsync<EtatExecutionInvalideException>(() => _service.ScannerComposantAsync(_preparation.Id, "CORPS-1;L1"));
        }

        [Fact]
        public async Task Terminer_ComposantManquantSansMotif_Refuse()
        {
            await _service.ScannerComposantAsync(_preparation.Id, "CORPS-1;L1");

            var ex = await Assert.ThrowsAsync<RegleMetierException>(() => _service.TerminerPreparationAsync(_preparation.Id, " "));
            Assert.Contains("JOINT-1", ex.Message);
            Assert.Equal(StatutsPreparationMagasin.EnCours, _preparation.Statut);
        }

        [Fact]
        public async Task Terminer_ComposantManquantAvecMotif_TermineEtGardeLeMotif()
        {
            await _service.ScannerComposantAsync(_preparation.Id, "CORPS-1;L1");

            await _service.TerminerPreparationAsync(_preparation.Id, "Joints en rupture, livrés demain");

            Assert.Equal(StatutsPreparationMagasin.Termine, _preparation.Statut);
            Assert.Equal("Joints en rupture, livrés demain", _preparation.IncompleteReason);
        }

        [Fact]
        public async Task Terminer_Complet_SansMotif()
        {
            await _service.ScannerComposantAsync(_preparation.Id, "CORPS-1;L1");
            await _service.ScannerComposantAsync(_preparation.Id, "JOINT-1;L2");

            await _service.TerminerPreparationAsync(_preparation.Id, "inutile");

            Assert.Equal(StatutsPreparationMagasin.Termine, _preparation.Statut);
            Assert.Null(_preparation.IncompleteReason);
        }

        [Fact]
        public async Task RetirerLot_SupprimeLeScan()
        {
            var scan = await _service.ScannerComposantAsync(_preparation.Id, "CORPS-1;L1");

            var dto = await _service.RetirerLotAsync(_preparation.Id, scan.Lot.Id);

            Assert.Equal(0, dto.ComposantsServis);
            _repository.Verify(r => r.SupprimerLot(It.IsAny<MagPreparationOfLot>()), Times.Once);
        }

        // ───────────── Expédition ─────────────

        private MagExpeditionBl Expedition()
        {
            var expedition = new MagExpeditionBl { Id = Guid.NewGuid(), NumeroBl = "BL1", Statut = StatutsExpeditionMagasin.EnCours, MatriculeMagasinier = Magasinier };
            _repository.Setup(r => r.GetExpeditionAsync(expedition.Id)).ReturnsAsync(expedition);
            _repository.Setup(r => r.GetBlAsync("BL1")).ReturnsAsync(new Sdelivery { NumeroBl = "BL1", CodeClient = "C1", StatutBl = "OUVERT" });
            return expedition;
        }

        [Fact]
        public async Task ScanOf_ProductionNonTerminee_Refuse()
        {
            var expedition = Expedition();
            _repository.Setup(r => r.ProductionTermineeAsync(NumeroOf)).ReturnsAsync(false);

            await Assert.ThrowsAsync<RegleMetierException>(() => _service.ScannerOfAsync(expedition.Id, NumeroOf));
            _repository.Verify(r => r.AjouterOfExpedie(It.IsAny<MagExpeditionBlScanOf>()), Times.Never);
        }

        [Fact]
        public async Task ScanOf_DejaSurCeBl_409()
        {
            var expedition = Expedition();
            expedition.MagExpeditionBlScanOfs.Add(new MagExpeditionBlScanOf { Id = Guid.NewGuid(), NumeroOfscanne = NumeroOf });

            await Assert.ThrowsAsync<ScanEnDoubleException>(() => _service.ScannerOfAsync(expedition.Id, NumeroOf));
            _repository.Verify(r => r.AjouterOfExpedie(It.IsAny<MagExpeditionBlScanOf>()), Times.Never);
        }

        [Fact]
        public async Task ScanOf_Termine_Ajoute()
        {
            var expedition = Expedition();
            _repository.Setup(r => r.ProductionTermineeAsync(NumeroOf)).ReturnsAsync(true);

            await _service.ScannerOfAsync(expedition.Id, "of:" + NumeroOf);

            _repository.Verify(r => r.AjouterOfExpedie(It.Is<MagExpeditionBlScanOf>(s => s.NumeroOfscanne == NumeroOf && s.ExpeditionBlid == expedition.Id)), Times.Once);
        }

        [Fact]
        public async Task TerminerExpedition_SansOf_Refuse()
        {
            var expedition = Expedition();
            await Assert.ThrowsAsync<RegleMetierException>(() => _service.TerminerExpeditionAsync(expedition.Id));
        }
    }
}
