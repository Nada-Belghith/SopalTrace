using Moq;
using SopalTrace.Application.DTOs.QualityPlans.Referentiels;
using Xunit;
using Microsoft.Extensions.Logging;
using SopalTrace.Application.Services;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.DTOs.QualityPlans.Modeles;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace SopalTrace.Application.Tests.Services
{
    public class ModeleFabricationServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IFormulaireStructureService> _mockFormulaireStructureService;
        private readonly Mock<IFrequencyParserService> _mockFrequencyParserService;

        private readonly ModeleFabricationService _service;

        public ModeleFabricationServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockFormulaireStructureService = new Mock<IFormulaireStructureService>();
            _mockFrequencyParserService = new Mock<IFrequencyParserService>();

            _mockCurrentUserService.Setup(s => s.MatriculeRequis).Returns("USER123");

            _service = new ModeleFabricationService(
                _mockUnitOfWork.Object,
                _mockCurrentUserService.Object,
                _mockFormulaireStructureService.Object,
                _mockFrequencyParserService.Object
            );
        }

        public async Task CreerModeleAsync_ArchiveAncienActif_DoitIncrementerVersionSiSuperieur()
        {
            // Arrange
            var request = new CreateModeleRequestDto
            {
                Code = "MOD001",
                Libelle = "Modele Test",
                TypeRobinetCode = "ROB1",
                NatureComposantCode = "NAT",
                FamilleProduitCode = "FAM",
                OperationCode = "OP1"
            };

            var existingDoc = new DocumentEntete
            {
                Id = Guid.NewGuid(),
                TypeDocumentCode = TypesDocument.ModeleFabrication,
                Nom = "MOD001",
                NatureArticleCode = "NAT",
                FamilleProduitFiniCode = "FAM",
                OperationCode = "OP1",
                Version = 2,
                Statut = "ACTIF"
            };

            _mockUnitOfWork.Setup(u => u.ModeleFabricationEnteteRepository.GetByFiltersAsync("NAT", "OP1", "FAM", null))
                .ReturnsAsync(new List<DocumentEntete> { existingDoc });

            // Simuler formulaire inactif (pas de formulaire trouvé => formId null)
            var formStruct = new FormulaireStructureDto(Guid.NewGuid(), "FRM_001", "Desig", null, "EN_COURS_DE_FABRICATION", 3);
            _mockFormulaireStructureService.Setup(f => f.GetFormulaireByRoleAsync(It.IsAny<string>()))
                .ReturnsAsync(formStruct);

            DocumentEntete? savedModele = null;
            _mockUnitOfWork.Setup(u => u.ModeleFabricationEnteteRepository.AddAsync(It.IsAny<DocumentEntete>()))
                .Callback<DocumentEntete>(e => savedModele = e);

            // Act
            var resultId = await _service.CreateModelAsync(request);

            // Assert
            _mockUnitOfWork.Verify(u => u.ModeleFabricationEnteteRepository.UpdateAsync(existingDoc), Times.Once); // Ancien modèle doit être mis à jour (ARCHIVE)
            Assert.Equal("ARCHIVE", existingDoc.Statut);

            _mockUnitOfWork.Verify(u => u.ModeleFabricationEnteteRepository.AddAsync(It.IsAny<DocumentEntete>()), Times.Once); // Nouveau ajouté
            Assert.NotNull(savedModele);
            Assert.Equal(3, savedModele.Version); // Version du formulaire (sa version de départ se donne à la création du code)
            Assert.Equal("ACTIF", savedModele.Statut); // Actif par défaut car pas de formulaire parent
        }

        public async Task CreerModeleAsync_DoitHeriterVersionDePRC_Et_EtreActif()
        {
            // Arrange
            var request = new CreateModeleRequestDto
            {
                Code = "MOD001",
                Libelle = "Modele Test",
                TypeRobinetCode = "ROB1",
                NatureComposantCode = "NAT",
                FamilleProduitCode = "FAM",
                OperationCode = "OP1"
            };

            var existingDoc = new DocumentEntete
            {
                Id = Guid.NewGuid(),
                TypeDocumentCode = TypesDocument.ModeleFabrication,
                Nom = "MOD001",
                NatureArticleCode = "NAT",
                FamilleProduitFiniCode = "FAM",
                OperationCode = "OP1",
                Version = 1,
                Statut = "ACTIF"
            };

            _mockUnitOfWork.Setup(u => u.ModeleFabricationEnteteRepository.GetByFiltersAsync("NAT", "OP1", "FAM", null))
                .ReturnsAsync(new List<DocumentEntete> { existingDoc });

            var formStruct = new FormulaireStructureDto(Guid.NewGuid(), "FRM_001", "Desig", null, "EN_COURS_DE_FABRICATION", 5);
            _mockFormulaireStructureService.Setup(f => f.GetFormulaireByRoleAsync("EN_COURS_DE_FABRICATION"))
                .ReturnsAsync(formStruct);

            var formDetails = new RefFormulaire { Id = formStruct.Id, CodeReference = "FRM_001", Version = 5, Statut = "BROUILLON" }; // Même si PRC est brouillon
            _mockUnitOfWork.Setup(u => u.RefFormulaireRepository.GetByIdAsync(formStruct.Id))
                .ReturnsAsync(formDetails);

            DocumentEntete? savedModele = null;
            _mockUnitOfWork.Setup(u => u.ModeleFabricationEnteteRepository.AddAsync(It.IsAny<DocumentEntete>()))
                .Callback<DocumentEntete>(e => savedModele = e);

            // Act
            var resultId = await _service.CreateModelAsync(request);

            // Assert
            _mockUnitOfWork.Verify(u => u.ModeleFabricationEnteteRepository.UpdateAsync(existingDoc), Times.Once);
            Assert.Equal("ARCHIVE", existingDoc.Statut); // Ancien modèle devient ARCHIVE
            
            Assert.NotNull(savedModele);
            Assert.Equal(5, savedModele.Version); // Hérite de formDetails.Version (PRC)
            Assert.Equal("ACTIF", savedModele.Statut); // Toujours ACTIF, ignore le BROUILLON de PRC
        }
    }
}
