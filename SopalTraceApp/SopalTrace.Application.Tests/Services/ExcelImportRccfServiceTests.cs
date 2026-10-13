using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Moq;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;
using SopalTrace.Infrastructure.Services.ExcelImport;
using Xunit;

namespace SopalTrace.Application.Tests.Services
{
    /// <summary>Import du plan « résultats du contrôle en cours de fabrication » (ClosedXML, plus d'EPPlus).</summary>
    public class ExcelImportRccfServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly Mock<IDictionnaireQualiteRepository> _dictionnaire = new();
        private readonly ExcelImportRccfService _service;

        public ExcelImportRccfServiceTests()
        {
            _unitOfWork.Setup(u => u.DictionnaireQualiteRepository).Returns(_dictionnaire.Object);
            _dictionnaire.Setup(d => d.GetAllTypeSectionsAsync()).ReturnsAsync(new System.Collections.Generic.List<TypeSection>());
            _dictionnaire.Setup(d => d.GetAllPeriodicitesAsync()).ReturnsAsync(new System.Collections.Generic.List<Periodicite>());
            _service = new ExcelImportRccfService(_unitOfWork.Object);
        }

        private static MemoryStream Classeur(Action<IXLWorksheet> remplir)
        {
            var flux = new MemoryStream();
            using (var classeur = new XLWorkbook())
            {
                remplir(classeur.AddWorksheet("Plan"));
                classeur.SaveAs(flux);
            }
            flux.Position = 0;
            return flux;
        }

        [Fact]
        public async Task Import_LitLesSectionsEtLesLignesAvecLeTexteAffiche()
        {
            using var flux = Classeur(f =>
            {
                f.Cell(1, 1).Value = "Caractéristiques à contrôler aux réglages (une série de 04 pièces)";
                f.Cell(2, 1).Value = "Caractéristiques contrôlées";
                f.Cell(3, 1).Value = "Diamètre";
                f.Cell(3, 2).Value = 12.5;
                f.Cell(3, 2).Style.NumberFormat.Format = "0.00";
                f.Cell(3, 3).Value = "Mesure";
                f.Cell(3, 4).Value = "Pied à coulisse";
                f.Cell(3, 6).Value = "Au démarrage";
                f.Cell(4, 1).Value = "Caractéristiques à contrôler au niveau du poste";
                f.Cell(5, 1).Value = "Étanchéité";
                f.Cell(5, 3).Value = "Visuel";
                f.Cell(6, 1).Value = "Remarque : contrôler aussi l'aspect";
            });

            var resultat = await _service.ImportAssemblageExcelAsync(flux);

            Assert.Equal(new[] { "REGLAGE", "LOT_POSTE" }, resultat.Sections.Select(s => s.SectionType));
            var reglage = Assert.Single(resultat.Sections[0].Lignes);
            Assert.Equal(("Diamètre", "12.50", "Au démarrage"), (reglage.Caracteristique, reglage.LimiteSpecTexte!.Replace(',', '.'), reglage.Observations));
            Assert.Equal("Étanchéité", Assert.Single(resultat.Sections[1].Lignes).Caracteristique);
            Assert.Contains("Remarque", resultat.Remarques);
        }

        [Fact]
        public async Task Import_ElementsDeDictionnaireCrees_AvecDesCodesUniques()
        {
            TypeCaracteristique? premier = null, second = null;
            _dictionnaire.Setup(d => d.AddTypeCaracteristiqueAsync(It.IsAny<TypeCaracteristique>()))
                .Callback<TypeCaracteristique>(t => { if (premier == null) premier = t; else second = t; })
                .Returns(Task.CompletedTask);

            // Deux libellés dont les 20 premiers caractères sont identiques : leurs codes doivent différer.
            using var flux = Classeur(f =>
            {
                f.Cell(1, 1).Value = "Contrôle de la longueur totale du corps A";
                f.Cell(2, 1).Value = "Contrôle de la longueur totale du corps B";
            });

            await _service.ImportAssemblageExcelAsync(flux);

            Assert.NotNull(second);
            Assert.NotEqual(premier!.Code, second!.Code);
        }

        [Fact]
        public async Task Import_ClasseurVide_SansErreurServeur()
        {
            using var flux = Classeur(_ => { });
            var resultat = await _service.ImportAssemblageExcelAsync(flux);
            Assert.Empty(resultat.Sections);
        }
    }
}
