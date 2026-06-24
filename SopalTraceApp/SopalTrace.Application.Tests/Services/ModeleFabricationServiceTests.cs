using Moq;
using SopalTrace.Application.DTOs.QualityPlans.Referentiels;
using Xunit;
using Microsoft.Extensions.Logging;
using SopalTrace.Application.Services;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.DTOs.QualityPlans.Modeles;
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

        private readonly ModeleFabricationService _service;

        public ModeleFabricationServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockFormulaireStructureService = new Mock<IFormulaireStructureService>();

            _mockCurrentUserService.Setup(s => s.UserInfo).Returns("USER123");

            _service = new ModeleFabricationService(
                _mockUnitOfWork.Object,
                _mockCurrentUserService.Object,
                _mockFormulaireStructureService.Object
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
                OperationCode = "OP1",
                VersionInitiale = 3 // Demande version 3
            };

            var existingDoc = new ModeleFabricationEntete
            {
                Id = Guid.NewGuid(),
                Code = "MOD001",
                NatureArticleCode = "NAT",
                FamilleProduitFiniCode = "FAM",
                OperationCode = "OP1",
                Version = 2,
                Statut = "ACTIF"
            };

            _mockUnitOfWork.Setup(u => u.ModeleFabricationEnteteRepository.GetByFiltersAsync("NAT", "OP1", "FAM"))
                .ReturnsAsync(new List<ModeleFabricationEntete> { existingDoc });

            // Simuler formulaire inactif (pas de formulaire trouvé => formId null)
            var formStruct = new FormulaireStructureDto(Guid.NewGuid(), "FRM_001", "Desig", null, "EN_COURS_DE_FABRICATION", 3);
            _mockFormulaireStructureService.Setup(f => f.GetFormulaireByRoleAsync(It.IsAny<string>()))
                .ReturnsAsync(formStruct);

            ModeleFabricationEntete? savedModele = null;
            _mockUnitOfWork.Setup(u => u.ModeleFabricationEnteteRepository.AddAsync(It.IsAny<ModeleFabricationEntete>()))
                .Callback<ModeleFabricationEntete>(e => savedModele = e);

            // Act
            var resultId = await _service.CreerModeleAsync(request);

            // Assert
            _mockUnitOfWork.Verify(u => u.ModeleFabricationEnteteRepository.UpdateAsync(existingDoc), Times.Once); // Ancien modèle doit être mis à jour (ARCHIVE)
            Assert.Equal("ARCHIVE", existingDoc.Statut);

            _mockUnitOfWork.Verify(u => u.ModeleFabricationEnteteRepository.AddAsync(It.IsAny<ModeleFabricationEntete>()), Times.Once); // Nouveau ajouté
            Assert.NotNull(savedModele);
            Assert.Equal(3, savedModele.Version); // VersionInitiale a pris le relais (3 > 2)
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

            var existingDoc = new ModeleFabricationEntete
            {
                Id = Guid.NewGuid(),
                Code = "MOD001",
                NatureArticleCode = "NAT",
                FamilleProduitFiniCode = "FAM",
                OperationCode = "OP1",
                Version = 1,
                Statut = "ACTIF"
            };

            _mockUnitOfWork.Setup(u => u.ModeleFabricationEnteteRepository.GetByFiltersAsync("NAT", "OP1", "FAM"))
                .ReturnsAsync(new List<ModeleFabricationEntete> { existingDoc });

            var formStruct = new FormulaireStructureDto(Guid.NewGuid(), "FRM_001", "Desig", null, "EN_COURS_DE_FABRICATION", 5);
            _mockFormulaireStructureService.Setup(f => f.GetFormulaireByRoleAsync("EN_COURS_DE_FABRICATION"))
                .ReturnsAsync(formStruct);

            var formDetails = new RefFormulaire { Id = formStruct.Id, CodeReference = "FRM_001", Version = 5, Statut = "BROUILLON" }; // Même si PRC est brouillon
            _mockUnitOfWork.Setup(u => u.RefFormulaireRepository.GetByIdAsync(formStruct.Id))
                .ReturnsAsync(formDetails);

            ModeleFabricationEntete? savedModele = null;
            _mockUnitOfWork.Setup(u => u.ModeleFabricationEnteteRepository.AddAsync(It.IsAny<ModeleFabricationEntete>()))
                .Callback<ModeleFabricationEntete>(e => savedModele = e);

            // Act
            var resultId = await _service.CreerModeleAsync(request);

            // Assert
            _mockUnitOfWork.Verify(u => u.ModeleFabricationEnteteRepository.UpdateAsync(existingDoc), Times.Once);
            Assert.Equal("ARCHIVE", existingDoc.Statut); // Ancien modèle devient ARCHIVE
            
            Assert.NotNull(savedModele);
            Assert.Equal(5, savedModele.Version); // Hérite de formDetails.Version (PRC)
            Assert.Equal("ACTIF", savedModele.Statut); // Toujours ACTIF, ignore le BROUILLON de PRC
        }
    }
}
