using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using SopalTrace.Application.DTOs.QualityPlans.ImportExcel;
using SopalTrace.Application.Helpers;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Services;
using Xunit;

namespace SopalTrace.Application.Tests.Services
{
    /// <summary>
    /// Le type de section porte seul la règle de contrôle ; la périodicité, facultative, n'est reconnue
    /// que pour un texte de fréquence (plus de table de règles d'échantillonnage).
    /// </summary>
    public class TypesSectionEtFrequencesTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly Mock<IDictionnaireQualiteRepository> _dictionnaire = new();
        private readonly FrequencyParserService _service;

        public TypesSectionEtFrequencesTests()
        {
            _unitOfWork.Setup(u => u.DictionnaireQualiteRepository).Returns(_dictionnaire.Object);
            _dictionnaire.Setup(d => d.GetAllPeriodicitesAsync()).ReturnsAsync(new List<Periodicite>
            {
                new() { Id = Guid.NewGuid(), NbrPieces = 12, Unite = "SERIE", Actif = true }
            });
            _dictionnaire.Setup(d => d.GetAllTypeSectionsAsync()).ReturnsAsync(new List<TypeSection>());
            _service = new FrequencyParserService(_unitOfWork.Object);
        }

        [Theory]
        [InlineData(SectionTypes.Echantillonnage3789, true)]
        [InlineData(SectionTypes.Echantillonnage2859, true)]
        [InlineData(SectionTypes.Echantillonnage3951, true)]
        [InlineData(SectionTypes.Reglage, false)]
        [InlineData(SectionTypes.LotOf, false)]
        [InlineData(null, false)]
        public void Echantillonnage_reconnu_par_le_type(string? type, bool attendu)
            => Assert.Equal(attendu, SectionTypes.EstEchantillonnage(type));

        [Theory]
        [InlineData("selon ISO 2859-1 : Tableau 2-A, avec un NQA de 0,65")]
        [InlineData("Selon FE0591 : Effectif de l'échantillon /poste (A/B) (p/h)")]
        [InlineData("la première et la dernière pièce et 0,01% du lot")]
        public async Task Reference_de_norme_sans_periodicite(string texte)
        {
            var section = new ImportExcelSectionDto();

            await _service.ParseFrequencyAsync(section, texte);

            Assert.Equal("SANS", section.ModeFreq);
            Assert.Null(section.PeriodiciteId);
            Assert.Equal(texte, section.FrequenceLibelle);
            _dictionnaire.Verify(d => d.AddPeriodiciteAsync(It.IsAny<Periodicite>()), Times.Never);
        }

        [Fact]
        public async Task Frequence_reconnue_devient_periodicite()
        {
            var section = new ImportExcelSectionDto();

            await _service.ParseFrequencyAsync(section, "une série de 12 pièces");

            Assert.Equal("VARIABLE", section.ModeFreq);
            Assert.NotNull(section.PeriodiciteId);
            Assert.Equal((12, "SERIE"), (section.FreqNum, section.TypeVariable));
        }

        [Fact]
        public async Task Enregistrement_libelle_de_section_aligne_sur_le_type_et_la_periodicite()
        {
            var poste = new TypeSection { Id = Guid.NewGuid(), TypeNormalise = SectionTypes.LotPoste, Libelle = "Contrôle par poste (1ère et dernière pièce)" };
            var reglage = new TypeSection { Id = Guid.NewGuid(), TypeNormalise = SectionTypes.Reglage, Libelle = "Contrôle aux réglages" };
            var serie12 = new Periodicite { Id = Guid.NewGuid(), NbrPieces = 12, Unite = "SERIE", Actif = true };
            _dictionnaire.Setup(d => d.GetAllTypeSectionsAsync()).ReturnsAsync(new List<TypeSection> { poste, reglage });
            _dictionnaire.Setup(d => d.GetAllPeriodicitesAsync()).ReturnsAsync(new List<Periodicite> { serie12 });

            var sansTitre = new DocumentSection { TypeSectionId = poste.Id, LibelleSection = "Caractéristiques à contrôler" };
            var avecPeriodicite = new DocumentSection { TypeSectionId = reglage.Id, PeriodiciteId = serie12.Id, LibelleSection = "Contrôle aux réglages test" };
            var libre = new DocumentSection { LibelleSection = "  Emballage  " };

            await ReferencesLignesQualite.CompleterAsync(new[] { sansTitre, avecPeriodicite, libre }, _dictionnaire.Object);

            Assert.Equal("Caractéristiques à contrôler Contrôle par poste (1ère et dernière pièce)", sansTitre.LibelleSection);
            Assert.Equal("Caractéristiques à contrôler Contrôle aux réglages (une série de 12 pièces)", avecPeriodicite.LibelleSection);
            Assert.Equal("  Emballage  ", libre.LibelleSection);
        }
    }
}
