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

        private readonly PlanFabricationService _service;

        public PlanFabricationServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockFormulaireStructureService = new Mock<IFormulaireStructureService>();
            _mockFrequencyParserService = new Mock<IFrequencyParserService>();

            _mockCurrentUserService.Setup(s => s.UserInfo).Returns("USER456");

            _service = new PlanFabricationService(
                _mockUnitOfWork.Object,
                _mockCurrentUserService.Object,
                _mockFormulaireStructureService.Object,
                _mockFrequencyParserService.Object
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

            var existingDoc = new PlanFabricationEntete
            {
                Id = Guid.NewGuid(),
                CodeArticleSageVersionne = "PLAN001",
                OperationCode = "OP1",
                Version = 1,
                Statut = "ACTIF" // L'existant est actif
            };

            _mockUnitOfWork.Setup(u => u.PlanFabricationEnteteRepository.GetByFiltersAsync("OP1"))
                .ReturnsAsync(new List<PlanFabricationEntete> { existingDoc });

            var formStruct = new FormulaireStructureDto(Guid.NewGuid(), "FRM_002", "Desig", null, "EN_COURS_DE_FABRICATION", 4);
            _mockFormulaireStructureService.Setup(f => f.GetFormulaireByRoleAsync("EN_COURS_DE_FABRICATION"))
                .ReturnsAsync(formStruct);

            var formDetails = new RefFormulaire { Id = formStruct.Id, CodeReference = "FRM_002", Version = 4, Statut = "BROUILLON" }; // Même si PRC est brouillon
            _mockUnitOfWork.Setup(u => u.RefFormulaireRepository.GetByIdAsync(formStruct.Id))
                .ReturnsAsync(formDetails);

            PlanFabricationEntete? savedPlan = null;
            _mockUnitOfWork.Setup(u => u.PlanFabricationEnteteRepository.AddAsync(It.IsAny<PlanFabricationEntete>()))
                .Callback<PlanFabricationEntete>(e => savedPlan = e);

            // Act
            var resultId = await _service.CreerPlanAsync(request);

            // Assert
            Assert.Equal("ARCHIVE", existingDoc.Statut); // Ancien plan doit passer en ARCHIVE

            _mockUnitOfWork.Verify(u => u.PlanFabricationEnteteRepository.AddAsync(It.IsAny<PlanFabricationEntete>()), Times.Once);
            Assert.NotNull(savedPlan);
            Assert.Equal(4, savedPlan.Version); // Version héritée du formulaire (4)
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
            
            PlanFabricationEntete? savedPlan = null;
            _mockUnitOfWork.Setup(u => u.PlanFabricationEnteteRepository.AddAsync(It.IsAny<PlanFabricationEntete>()))
                .Callback<PlanFabricationEntete>(e => savedPlan = e);
                
            var formStruct = new FormulaireStructureDto(Guid.NewGuid(), "FRM_002", "Desig", null, "EN_COURS_DE_FABRICATION", 4);
            _mockFormulaireStructureService.Setup(f => f.GetFormulaireByRoleAsync("EN_COURS_DE_FABRICATION"))
                .ReturnsAsync(formStruct);
            _mockUnitOfWork.Setup(u => u.RefFormulaireRepository.GetByIdAsync(formStruct.Id))
                .ReturnsAsync(new RefFormulaire { Id = formStruct.Id, CodeReference = "FRM_002", Version = 4 });
                
            _mockUnitOfWork.Setup(u => u.PlanFabricationEnteteRepository.GetByFiltersAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<PlanFabricationEntete>());

            // Act
            var resultId = await _service.CreerPlanAsync(request);

            // Assert
            Assert.NotNull(savedPlan);
            Assert.Equal(modeleId, savedPlan.ModeleSourceId);
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task MettreAJourPlanAsync_ImportExcel_DoitRemplacerSectionsEtLignes()
        {
            // Arrange
            var planId = Guid.NewGuid();
            var existingPlan = new PlanFabricationEntete { Id = planId, PlanFabricationSections = new List<PlanFabricationSection>() };
            var request = new UpdatePlanFabricationRequestDto
            {
                Sections = new List<CreatePlanFabricationSectionDto>
                {
                    new CreatePlanFabricationSectionDto { LibelleSection = "Sec 1", Lignes = new List<CreatePlanFabricationLigneDto> { new CreatePlanFabricationLigneDto { Instruction = "L1" } } }
                }
            };

            _mockUnitOfWork.Setup(u => u.PlanFabricationEnteteRepository.GetByIdAsync(planId, true))
                .ReturnsAsync(existingPlan);

            // Capture les sections ajoutées via AddSection (nouvelle stratégie d'insertion directe)
            var addedSections = new List<PlanFabricationSection>();
            _mockUnitOfWork.Setup(u => u.PlanFabricationEnteteRepository.AddSection(It.IsAny<PlanFabricationSection>()))
                .Callback<PlanFabricationSection>(s => addedSections.Add(s));

            var formStruct = new FormulaireStructureDto(Guid.NewGuid(), "FRM_002", "Desig", null, "EN_COURS_DE_FABRICATION", 4);
            _mockFormulaireStructureService.Setup(f => f.GetFormulaireByRoleAsync("EN_COURS_DE_FABRICATION"))
                .ReturnsAsync(formStruct);
            _mockUnitOfWork.Setup(u => u.RefFormulaireRepository.GetByIdAsync(formStruct.Id))
                .ReturnsAsync(new RefFormulaire { Id = formStruct.Id, CodeReference = "FRM_002", Version = 4 });

            // Act
            var result = await _service.MettreAJourPlanAsync(planId, request);

            // Assert
            Assert.True(result);
            // Vérifie qu'AddSection a été appelé une fois avec la bonne section + ses lignes
            Assert.Single(addedSections);
            Assert.Equal("Sec 1", addedSections.First().LibelleSection);
            Assert.Single(addedSections.First().PlanFabricationLignes);
            Assert.Equal("L1", addedSections.First().PlanFabricationLignes.First().Instruction);
            _mockUnitOfWork.Verify(u => u.PlanFabricationEnteteRepository.AddSection(It.IsAny<PlanFabricationSection>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task CreerNouvelleVersionPlanAsync_DoitClonerPlanExistant()
        {
            // Arrange
            var existingId = Guid.NewGuid();
            var existingPlan = new PlanFabricationEntete
            {
                Id = existingId, CodeArticleSageVersionne = "ART1", Version = 1,
                PlanFabricationSections = new List<PlanFabricationSection>
                {
                    new PlanFabricationSection { Id = Guid.NewGuid(), LibelleSection = "Old Sec", PlanFabricationLignes = new List<PlanFabricationLigne> { new PlanFabricationLigne { Instruction = "Old L1" } } }
                }
            };
            
            _mockUnitOfWork.Setup(u => u.PlanFabricationEnteteRepository.GetByIdAsync(existingId, true))
                .ReturnsAsync(existingPlan);
            _mockUnitOfWork.Setup(u => u.PlanFabricationEnteteRepository.GetLatestVersionAsync("ART1", It.IsAny<string>()))
                .ReturnsAsync(1);
            _mockUnitOfWork.Setup(u => u.PlanFabricationEnteteRepository.GetByFiltersAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<PlanFabricationEntete> { existingPlan });

            var formStruct = new FormulaireStructureDto(Guid.NewGuid(), "FRM_002", "Desig", null, "EN_COURS_DE_FABRICATION", 2);
            _mockFormulaireStructureService.Setup(f => f.GetFormulaireByRoleAsync("EN_COURS_DE_FABRICATION"))
                .ReturnsAsync(formStruct);
            _mockUnitOfWork.Setup(u => u.RefFormulaireRepository.GetByIdAsync(formStruct.Id))
                .ReturnsAsync(new RefFormulaire { Id = formStruct.Id, CodeReference = "FRM_002", Version = 2 });
            
            PlanFabricationEntete? savedPlan = null;
            _mockUnitOfWork.Setup(u => u.PlanFabricationEnteteRepository.AddAsync(It.IsAny<PlanFabricationEntete>()))
                .Callback<PlanFabricationEntete>(e => savedPlan = e);
                
            var request = new NouvelleVersionPlanFabricationRequestDto { AncienId = existingId };

            // Act
            var newId = await _service.CreerNouvelleVersionPlanAsync(request);

            // Assert
            Assert.NotNull(savedPlan);
            Assert.Equal(2, savedPlan.Version);
            Assert.Single(savedPlan.PlanFabricationSections);
            Assert.Single(savedPlan.PlanFabricationSections.First().PlanFabricationLignes);
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task RestaurerPlanArchiveAsync_DoitClonerPlanEtLeRendreActif()
        {
            // Arrange
            var archiveId = Guid.NewGuid();
            var archivePlan = new PlanFabricationEntete
            {
                Id = archiveId, CodeArticleSageVersionne = "ART1", Version = 1, Statut = "ARCHIVE",
                PlanFabricationSections = new List<PlanFabricationSection> { new PlanFabricationSection { LibelleSection = "Archive Sec", PlanFabricationLignes = new List<PlanFabricationLigne> { new PlanFabricationLigne { Instruction = "Archive L1" } } } }
            };
            
            _mockUnitOfWork.Setup(u => u.PlanFabricationEnteteRepository.GetByIdAsync(archiveId, true))
                .ReturnsAsync(archivePlan);
            _mockUnitOfWork.Setup(u => u.PlanFabricationEnteteRepository.GetLatestVersionAsync("ART1", It.IsAny<string>()))
                .ReturnsAsync(2); // Simule qu'une version 2 existe déjà
            _mockUnitOfWork.Setup(u => u.PlanFabricationEnteteRepository.GetByFiltersAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<PlanFabricationEntete> { archivePlan });

            var formStruct = new FormulaireStructureDto(Guid.NewGuid(), "FRM_002", "Desig", null, "EN_COURS_DE_FABRICATION", 3);
            _mockFormulaireStructureService.Setup(f => f.GetFormulaireByRoleAsync("EN_COURS_DE_FABRICATION"))
                .ReturnsAsync(formStruct);
            _mockUnitOfWork.Setup(u => u.RefFormulaireRepository.GetByIdAsync(formStruct.Id))
                .ReturnsAsync(new RefFormulaire { Id = formStruct.Id, CodeReference = "FRM_002", Version = 3 });
                
            PlanFabricationEntete? savedPlan = null;
            _mockUnitOfWork.Setup(u => u.PlanFabricationEnteteRepository.AddAsync(It.IsAny<PlanFabricationEntete>()))
                .Callback<PlanFabricationEntete>(e => savedPlan = e);

            var request = new RestaurerDocumentRequestDto { DocumentArchiveId = archiveId };

            // Act
            var newId = await _service.RestaurerPlanArchiveAsync(request);

            // Assert
            Assert.NotNull(savedPlan);
            Assert.Equal(3, savedPlan.Version);
            Assert.Equal("ACTIF", savedPlan.Statut);
            Assert.Single(savedPlan.PlanFabricationSections);
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task GetPlanByIdAsync_VerificationExistenceLignes()
        {
            // Arrange
            var planId = Guid.NewGuid();
            var plan = new PlanFabricationEntete
            {
                Id = planId,
                PlanFabricationSections = new List<PlanFabricationSection>
                {
                    new PlanFabricationSection
                    {
                        Id = Guid.NewGuid(),
                        PlanFabricationLignes = new List<PlanFabricationLigne>
                        {
                            new PlanFabricationLigne { Id = Guid.NewGuid(), Instruction = "Test Ligne" }
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
