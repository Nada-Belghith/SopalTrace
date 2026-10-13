using Moq;
using SopalTrace.Application.DTOs.QualityPlans.Referentiels;
using Xunit;
using Microsoft.Extensions.Logging;
using SopalTrace.Application.Services;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.DTOs.QualityPlans.Documents;
using SopalTrace.Application.DTOs.QualityPlans.Fabrication;
using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace SopalTrace.Application.Tests.Services
{
    public class PlanFabricationServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IFormulaireStructureService> _mockFormulaireStructureService;
        private readonly Mock<IFrequencyParserService> _mockFrequencyParserService;
        private readonly Mock<IEmailService> _mockEmailService;

        private readonly PlanFabricationService _service;

        public PlanFabricationServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockFormulaireStructureService = new Mock<IFormulaireStructureService>();
            _mockFrequencyParserService = new Mock<IFrequencyParserService>();
            _mockEmailService = new Mock<IEmailService>();
            // Référentiels complétés à l'enregistrement (caractéristiques, instruments manquants).
            _mockUnitOfWork.Setup(u => u.DictionnaireQualiteRepository).Returns(new Mock<IDictionnaireQualiteRepository>().Object);

            _mockCurrentUserService.Setup(s => s.MatriculeRequis).Returns("USER456");

            _service = new PlanFabricationService(
                _mockUnitOfWork.Object,
                _mockCurrentUserService.Object,
                _mockFormulaireStructureService.Object,
                _mockFrequencyParserService.Object,
                _mockEmailService.Object
            );
        }

        [Fact]
        public async Task CreerPlanAsync_ArchiveAncienActif_DoitEtreActifEtHeriterVersion()
        {
            // Arrange
            var request = new CreatePlanFabricationRequestDto
            {
                Nom = "PLAN001",
                OperationCode = "OP1"
            };

            var existingDoc = new DocumentEntete
            {
                Id = Guid.NewGuid(),
                CodeArticleSageVersionne = "PLAN001",
                OperationCode = "OP1",
                Version = 1,
                Statut = "ACTIF" // L'existant est actif
            };

            _mockUnitOfWork.Setup(u => u.PlanFabricationEnteteRepository.GetByFiltersAsync("OP1", null))
                .ReturnsAsync(new List<DocumentEntete> { existingDoc });

            var formStruct = new FormulaireStructureDto(Guid.NewGuid(), "FRM_002", "Desig", null, "EN_COURS_DE_FABRICATION", 4);
            _mockFormulaireStructureService.Setup(f => f.GetFormulaireByRoleAsync("EN_COURS_DE_FABRICATION"))
                .ReturnsAsync(formStruct);

            var formDetails = new RefFormulaire { Id = formStruct.Id, CodeReference = "FRM_002", Version = 4, Statut = "BROUILLON" }; // Même si PRC est brouillon
            _mockUnitOfWork.Setup(u => u.RefFormulaireRepository.GetByIdAsync(formStruct.Id))
                .ReturnsAsync(formDetails);

            DocumentEntete? savedPlan = null;
            _mockUnitOfWork.Setup(u => u.PlanFabricationEnteteRepository.AddAsync(It.IsAny<DocumentEntete>()))
                .Callback<DocumentEntete>(e => savedPlan = e);

            // Act
            var resultId = await _service.CreatePlanAsync(request);

            // Assert
            Assert.Equal("ARCHIVE", existingDoc.Statut); // Ancien plan doit passer en ARCHIVE

            _mockUnitOfWork.Verify(u => u.PlanFabricationEnteteRepository.AddAsync(It.IsAny<DocumentEntete>()), Times.Once);
            Assert.NotNull(savedPlan);
            Assert.Equal(2, savedPlan.Version); // Version incrémentée depuis l'existant (1 -> 2)
            Assert.Equal("ACTIF", savedPlan.Statut); // Nouveau plan est toujours ACTIF
        }
        [Fact]
        public async Task CreerPlanAsync_AvecModeleSource_DoitInitialiserModeleSourceId()
        {
            // Arrange
            var modeleId = Guid.NewGuid();
            var request = new CreatePlanFabricationRequestDto
            {
                Nom = "PLAN_MODELE",
                OperationCode = "OP1",
                ModeleSourceId = modeleId
            };
            
            DocumentEntete? savedPlan = null;
            _mockUnitOfWork.Setup(u => u.PlanFabricationEnteteRepository.AddAsync(It.IsAny<DocumentEntete>()))
                .Callback<DocumentEntete>(e => savedPlan = e);
                
            var formStruct = new FormulaireStructureDto(Guid.NewGuid(), "FRM_002", "Desig", null, "EN_COURS_DE_FABRICATION", 4);
            _mockFormulaireStructureService.Setup(f => f.GetFormulaireByRoleAsync("EN_COURS_DE_FABRICATION"))
                .ReturnsAsync(formStruct);
            _mockUnitOfWork.Setup(u => u.RefFormulaireRepository.GetByIdAsync(formStruct.Id))
                .ReturnsAsync(new RefFormulaire { Id = formStruct.Id, CodeReference = "FRM_002", Version = 4 });
                
            _mockUnitOfWork.Setup(u => u.PlanFabricationEnteteRepository.GetByFiltersAsync(It.IsAny<string>(), null))
                .ReturnsAsync(new List<DocumentEntete>());

            // Act
            var resultId = await _service.CreatePlanAsync(request);

            // Assert
            Assert.NotNull(savedPlan);
            Assert.Equal(modeleId, savedPlan.DocumentSourceId);
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task MettreAJourPlanAsync_ImportExcel_DoitRemplacerSectionsEtLignes()
        {
            // Arrange
            var planId = Guid.NewGuid();
            var existingPlan = new DocumentEntete { Id = planId, DocumentSections = new List<DocumentSection>() };
            var request = new UpdatePlanFabricationRequestDto
            {
                Sections = new List<CreatePlanFabricationSectionDto>
                {
                    new CreatePlanFabricationSectionDto { LibelleSection = "Sec 1", Lignes = new List<CreatePlanFabricationLigneDto> { new CreatePlanFabricationLigneDto { Instruction = "L1" } } }
                }
            };

            _mockUnitOfWork.Setup(u => u.PlanFabricationEnteteRepository.GetByIdAsync(planId, true))
                .ReturnsAsync(existingPlan);

            var formStruct = new FormulaireStructureDto(Guid.NewGuid(), "FRM_002", "Desig", null, "EN_COURS_DE_FABRICATION", 4);
            _mockFormulaireStructureService.Setup(f => f.GetFormulaireByRoleAsync("EN_COURS_DE_FABRICATION"))
                .ReturnsAsync(formStruct);
            _mockUnitOfWork.Setup(u => u.RefFormulaireRepository.GetByIdAsync(formStruct.Id))
                .ReturnsAsync(new RefFormulaire { Id = formStruct.Id, CodeReference = "FRM_002", Version = 4 });

            // Act
            var result = await _service.UpdatePlanAsync(existingPlan.Id, request);

            // Assert
            Assert.NotNull(result); // nouvelle version du document
            // Les nouvelles sections sont ajoutées à la collection suivie du plan (SectionUpdateHelper)
            var section = Assert.Single(existingPlan.DocumentSections);
            Assert.Equal("Sec 1", section.LibelleSection);
            Assert.Equal("L1", Assert.Single(section.DocumentLignes).Instruction);
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task CreerNouvelleVersionPlanAsync_DoitClonerPlanExistant()
        {
            // Arrange
            var existingId = Guid.NewGuid();
            var existingPlan = new DocumentEntete
            {
                Id = existingId, CodeArticleSageVersionne = "ART1", Version = 1,
                DocumentSections = new List<DocumentSection>
                {
                    new DocumentSection { Id = Guid.NewGuid(), LibelleSection = "Old Sec", DocumentLignes = new List<DocumentLigne> { new DocumentLigne { Instruction = "Old L1" } } }
                }
            };
            
            _mockUnitOfWork.Setup(u => u.PlanFabricationEnteteRepository.GetByIdAsync(existingId, true))
                .ReturnsAsync(existingPlan);
            _mockUnitOfWork.Setup(u => u.PlanFabricationEnteteRepository.GetLatestVersionAsync("ART1", It.IsAny<string>()))
                .ReturnsAsync(1);
            _mockUnitOfWork.Setup(u => u.PlanFabricationEnteteRepository.GetByFiltersAsync(It.IsAny<string>(), null))
                .ReturnsAsync(new List<DocumentEntete> { existingPlan });

            var formStruct = new FormulaireStructureDto(Guid.NewGuid(), "FRM_002", "Desig", null, "EN_COURS_DE_FABRICATION", 2);
            _mockFormulaireStructureService.Setup(f => f.GetFormulaireByRoleAsync("EN_COURS_DE_FABRICATION"))
                .ReturnsAsync(formStruct);
            _mockUnitOfWork.Setup(u => u.RefFormulaireRepository.GetByIdAsync(formStruct.Id))
                .ReturnsAsync(new RefFormulaire { Id = formStruct.Id, CodeReference = "FRM_002", Version = 2 });
            
            DocumentEntete? savedPlan = null;
            _mockUnitOfWork.Setup(u => u.PlanFabricationEnteteRepository.AddAsync(It.IsAny<DocumentEntete>()))
                .Callback<DocumentEntete>(e => savedPlan = e);
                
            var newVersionRequest = new NouvelleVersionPlanFabricationRequestDto { AncienId = existingId };

            // Act
            var result = await _service.CreateNewVersionAsync(newVersionRequest);

            // Assert
            Assert.NotNull(savedPlan);
            Assert.Equal(2, savedPlan.Version);
            Assert.Single(savedPlan.DocumentSections);
            Assert.Single(savedPlan.DocumentSections.First().DocumentLignes);
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }


        [Fact]
        public async Task GetPlanByIdAsync_VerificationExistenceLignes()
        {
            // Arrange
            var planId = Guid.NewGuid();
            var plan = new DocumentEntete
            {
                Id = planId,
                DocumentSections = new List<DocumentSection>
                {
                    new DocumentSection
                    {
                        Id = Guid.NewGuid(),
                        DocumentLignes = new List<DocumentLigne>
                        {
                            new DocumentLigne { Id = Guid.NewGuid(), Instruction = "Test Ligne" }
                        }
                    }
                }
            };
            
            _mockUnitOfWork.Setup(u => u.PlanFabricationEnteteRepository.GetByIdAsync(planId, true))
                .ReturnsAsync(plan);

            // Act
            var result = await _service.GetPlanByIdAsync(planId);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Sections);
            Assert.Single(result.Sections);
            Assert.NotNull(result.Sections.First().Lignes);
            Assert.Single(result.Sections.First().Lignes);
            Assert.Equal("Test Ligne", result.Sections.First().Lignes.First().Instruction);
        }
    }
}
