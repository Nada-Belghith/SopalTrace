using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Moq;
using SopalTrace.Application.DTOs.QualityPlans.ImportExcel;
using SopalTrace.Application.Helpers;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Services;
using SopalTrace.Infrastructure.Services.ExcelImport;
using Xunit;

namespace SopalTrace.Application.Tests.Services
{
    /// <summary>
    /// Rapprochement des lignes importées avec les référentiels : lecture seule pendant l'aperçu,
    /// création des éléments manquants seulement à l'enregistrement.
    /// </summary>
    public class ImportPlanExcelTests
    {
        private readonly Mock<IDictionnaireQualiteRepository> _dictionnaire = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();

        private static readonly MoyenControle BancEssai = new() { Id = Guid.NewGuid(), Code = "BANC_ESSAI", Libelle = "Banc d'essai", Actif = true };
        private static readonly MoyenControle PiedACoulisse = new() { Id = Guid.NewGuid(), Code = "PAC", Libelle = "P à C (Pied à coulisse)", Actif = true };
        private static readonly TypeControle Mesure = new() { Id = Guid.NewGuid(), Code = "MESURE", Libelle = "Mesure", Actif = true };
        private static readonly TypeControle Attribut = new() { Id = Guid.NewGuid(), Code = "ATTRIBUT", Libelle = "Attribut", Actif = true };
        private static readonly TypeCaracteristique Etancheite = new() { Id = Guid.NewGuid(), Code = "ETANCH", Libelle = "Étanchéité", Actif = true };

        public ImportPlanExcelTests()
        {
            _unitOfWork.Setup(u => u.DictionnaireQualiteRepository).Returns(_dictionnaire.Object);
            _dictionnaire.Setup(d => d.GetActiveTypeCaracteristiquesAsync()).ReturnsAsync(new List<TypeCaracteristique> { Etancheite });
            _dictionnaire.Setup(d => d.GetActiveTypeControlesAsync()).ReturnsAsync(new List<TypeControle> { Mesure, Attribut });
            _dictionnaire.Setup(d => d.GetActiveMoyenControlesAsync()).ReturnsAsync(new List<MoyenControle> { BancEssai, PiedACoulisse });
            _dictionnaire.Setup(d => d.GetActiveInstrumentsAsync()).ReturnsAsync(new List<Instrument> { new() { CodeInstrument = "BAN47", Designation = "BEE LAB : (BAN47)", Actif = true } });
            _dictionnaire.Setup(d => d.GetAllTypeSectionsAsync()).ReturnsAsync(new List<TypeSection>());
        }

        [Theory]
        [InlineData("BEE LAB : (BAN47)", "BAN47")]
        [InlineData("SER 05", "SER05")]
        [InlineData(" CLD03", "CLD03")]
        [InlineData("EN350030242-001", "EN350030242-001")]
        [InlineData("Marquage lisible", null)]
        [InlineData("Registre de traçabilité d’assemblage", null)]
        public void Code_instrument_normalise(string saisi, string? attendu)
            => Assert.Equal(attendu, ResolveurLignesPlan.NormaliserCodeInstrument(saisi));

        [Fact]
        public async Task Resolution_rapproche_sans_tenir_compte_des_accents_ni_des_apostrophes()
        {
            var resolveur = await ResolveurLignesPlan.ChargerAsync(_dictionnaire.Object);
            var avertissements = new List<string>();

            var ligne = resolveur.Resoudre(new LigneLue
            {
                LigneExcel = 4, Caracteristique = "etancheite", TypeControle = "MESURE",
                Moyen = "Banc d’essai", Instrument = "BEE LAB : (BAN47)"
            }, avertissements);
            var pac = resolveur.Resoudre(new LigneLue { LigneExcel = 5, Caracteristique = "Longueur", Moyen = "P à C" }, avertissements);

            Assert.Equal(Etancheite.Id, ligne.TypeCaracteristiqueId);
            Assert.Equal(Mesure.Id, ligne.TypeControleId);
            Assert.Equal(BancEssai.Id, ligne.MoyenControleId);
            Assert.Null(ligne.MoyenTexteLibre);
            Assert.Equal("BAN47", ligne.InstrumentCode);
            Assert.Equal(PiedACoulisse.Id, pac.MoyenControleId);
            Assert.Null(pac.TypeCaracteristiqueId);
            Assert.Empty(avertissements);
            Assert.Empty(resolveur.Recapitulatif());
        }

        [Fact]
        public async Task Resolution_conserve_l_inconnu_en_texte_et_le_signale()
        {
            var resolveur = await ResolveurLignesPlan.ChargerAsync(_dictionnaire.Object);
            var avertissements = new List<string>();

            var ligne = resolveur.Resoudre(new LigneLue
            {
                LigneExcel = 14, Caracteristique = "Vérification du marquage", TypeControle = "Contrôle sonore",
                Moyen = "Suivant plan et défauthèque", Instrument = "Marquage lisible", Observations = "Sur le corps"
            }, avertissements);
            resolveur.Resoudre(new LigneLue { LigneExcel = 17, Caracteristique = "Etanchéité", Instrument = "BEE46" }, avertissements);

            Assert.Null(ligne.MoyenControleId);
            Assert.Equal("Suivant plan et défauthèque", ligne.MoyenTexteLibre);
            Assert.Null(ligne.TypeControleId);
            Assert.Equal(string.Empty, ligne.InstrumentCode);
            Assert.Equal("Marquage lisible\nSur le corps", ligne.Observations);
            Assert.Contains(avertissements, a => a.StartsWith("Ligne 14") && a.Contains("Marquage lisible"));
            var recap = resolveur.Recapitulatif().ToList();
            Assert.Contains(recap, r => r.Contains("Contrôle sonore"));
            Assert.Contains(recap, r => r.Contains("BEE46"));
        }

        [Fact]
        public async Task Apercu_d_import_n_ecrit_rien_dans_les_referentiels()
        {
            var frequences = new Mock<IFrequencyParserService>();
            var service = new ExcelImportService(_unitOfWork.Object, frequences.Object, Mock.Of<IExcelImportFactory>());

            using var flux = new MemoryStream();
            using (var classeur = new XLWorkbook())
            {
                var f = classeur.AddWorksheet("Plan");
                f.Cell("A1").Value = "Caractéristiques à contrôler aux réglages (une série de 5 pièces)";
                f.Range("A1:F1").Merge();
                var entete = new[] { "Caractéristique contrôlée", "Limite de spécification", "Type de contrôle", "Moyen de contrôle", "Code instrument de contrôle", "Observations" };
                for (var i = 0; i < entete.Length; i++) f.Cell(2, i + 1).Value = entete[i];
                f.Cell("A3").Value = "Nouvelle caractéristique"; f.Cell("C3").Value = "Attribut";
                f.Cell("D3").Value = "Machine de marquage"; f.Cell("E3").Value = "SER 05";
                classeur.SaveAs(flux);
            }
            flux.Position = 0;

            var resultat = await service.ParsePlanExcelAsync(flux, "plan.xlsx");

            Assert.True(resultat.Succes);
            Assert.Equal(1, resultat.LignesLues);
            var ligne = resultat.Sections.Single().Lignes.Single();
            Assert.Equal("Machine de marquage", ligne.MoyenTexteLibre);
            Assert.Equal("SER05", ligne.InstrumentCode);
            _dictionnaire.Verify(d => d.AddTypeCaracteristiqueAsync(It.IsAny<TypeCaracteristique>()), Times.Never);
            _dictionnaire.Verify(d => d.AddTypeControleAsync(It.IsAny<TypeControle>()), Times.Never);
            _dictionnaire.Verify(d => d.AddMoyenControleAsync(It.IsAny<MoyenControle>()), Times.Never);
            _dictionnaire.Verify(d => d.AddInstrumentAsync(It.IsAny<Instrument>()), Times.Never);
            _unitOfWork.Verify(u => u.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task Fichier_non_reconnu_renvoie_une_erreur_et_pas_d_exception()
        {
            var service = new ExcelImportService(_unitOfWork.Object, Mock.Of<IFrequencyParserService>(), Mock.Of<IExcelImportFactory>());
            using var flux = new MemoryStream();
            using (var classeur = new XLWorkbook())
            {
                classeur.AddWorksheet("Vide").Cell("A1").Value = "Rien à voir";
                classeur.SaveAs(flux);
            }
            flux.Position = 0;

            var resultat = await service.ParsePlanExcelAsync(flux, "autre.xlsx");

            Assert.False(resultat.Succes);
            Assert.NotEmpty(resultat.Erreurs);
            Assert.Empty(resultat.Sections);
        }

        // ---------- Enregistrement : complément des référentiels ----------

        private static DocumentSection Section(params DocumentLigne[] lignes)
            => new() { Id = Guid.NewGuid(), DocumentLignes = lignes.ToList() };

        [Fact]
        public async Task Enregistrement_cree_une_seule_fois_une_caracteristique_repetee()
        {
            _dictionnaire.Setup(d => d.GetTypeCaracteristiqueByLibelleAsync(It.IsAny<string>())).ReturnsAsync((TypeCaracteristique?)null);
            var l1 = new DocumentLigne { LibelleAffiche = "Marquage du corps" };
            var l2 = new DocumentLigne { LibelleAffiche = "Marquage  du corps" };

            await ReferencesLignesQualite.CompleterAsync(new[] { Section(l1), Section(l2) }, _dictionnaire.Object);

            Assert.NotNull(l1.TypeCaracteristiqueId);
            Assert.Equal(l1.TypeCaracteristiqueId, l2.TypeCaracteristiqueId);
            _dictionnaire.Verify(d => d.AddTypeCaracteristiqueAsync(It.IsAny<TypeCaracteristique>()), Times.Once);
        }

        [Fact]
        public async Task Enregistrement_libelle_long_recherche_et_cree_sur_80_caracteres()
        {
            var libelle = new string('x', 120);
            _dictionnaire.Setup(d => d.GetTypeCaracteristiqueByLibelleAsync(It.IsAny<string>())).ReturnsAsync((TypeCaracteristique?)null);
            TypeCaracteristique? cree = null;
            _dictionnaire.Setup(d => d.AddTypeCaracteristiqueAsync(It.IsAny<TypeCaracteristique>()))
                .Callback<TypeCaracteristique>(t => cree = t).Returns(Task.CompletedTask);

            await ReferencesLignesQualite.CompleterAsync(new[] { Section(new DocumentLigne { LibelleAffiche = libelle }) }, _dictionnaire.Object);

            Assert.Equal(80, cree!.Libelle.Length);
            _dictionnaire.Verify(d => d.GetTypeCaracteristiqueByLibelleAsync(new string('x', 80)), Times.Once);
        }

        [Fact]
        public async Task Enregistrement_moyen_libre_rattache_au_catalogue_sans_violer_la_contrainte_XOR()
        {
            var connu = new DocumentLigne { MoyenTexteLibre = "banc d’essai" };
            var inconnu = new DocumentLigne { MoyenTexteLibre = "Machine de marquage" };
            var lesDeux = new DocumentLigne { MoyenControleId = PiedACoulisse.Id, MoyenTexteLibre = "P à C" };

            await ReferencesLignesQualite.CompleterAsync(new[] { Section(connu, inconnu, lesDeux) }, _dictionnaire.Object);

            Assert.Equal((BancEssai.Id, (string?)null), (connu.MoyenControleId!.Value, connu.MoyenTexteLibre));
            Assert.Equal(((Guid?)null, "Machine de marquage"), (inconnu.MoyenControleId, inconnu.MoyenTexteLibre));
            Assert.Null(lesDeux.MoyenTexteLibre);
            _dictionnaire.Verify(d => d.AddMoyenControleAsync(It.IsAny<MoyenControle>()), Times.Never);
        }

        [Fact]
        public async Task Enregistrement_cree_l_instrument_manquant_une_seule_fois()
        {
            _dictionnaire.Setup(d => d.GetInstrumentByCodeAsync("BEE46")).ReturnsAsync((Instrument?)null);
            _dictionnaire.Setup(d => d.GetInstrumentByCodeAsync("BAN47")).ReturnsAsync(new Instrument { CodeInstrument = "BAN47", Designation = "x" });

            await ReferencesLignesQualite.CompleterAsync(new[]
            {
                Section(new DocumentLigne { InstrumentCode = "BEE46" }, new DocumentLigne { InstrumentCode = " BEE46 " }, new DocumentLigne { InstrumentCode = "BAN47" })
            }, _dictionnaire.Object);

            _dictionnaire.Verify(d => d.AddInstrumentAsync(It.Is<Instrument>(i => i.CodeInstrument == "BEE46")), Times.Once);
            _dictionnaire.Verify(d => d.AddInstrumentAsync(It.Is<Instrument>(i => i.CodeInstrument == "BAN47")), Times.Never);
        }
    }
}
