using Moq;
using SopalTrace.Application.DTOs.QualityPlans.Referentiels;
using Xunit;
using Microsoft.Extensions.Logging;
using SopalTrace.Application.Services;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.DTOs.QualityPlans.Documents;
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

        private readonly PlanFabricationService _service;

        public PlanFabricationServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockFormulaireStructureService = new Mock<IFormulaireStructureService>();

            _mockCurrentUserService.Setup(s => s.UserInfo).Returns("USER456");

            _service = new PlanFabricationService(
                _mockUnitOfWork.Object,
                _mockCurrentUserService.Object,
                _mockFormulaireStructureService.Object
            );
        }

        [Fact]
        public async Task CreerPlanAsync_ArchiveAncienActif_DoitEtreActifEtHeriterVersion()
        {
            // Arrange
            var request = new CreateDocumentRequestDto
            {
                TypeDocumentCode = "PLAN_FAB",
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
            _mockUnitOfWork.Verify(u => u.PlanFabricationEnteteRepository.UpdateAsync(existingDoc), Times.Once); 
            Assert.Equal("ARCHIVE", existingDoc.Statut); // Ancien plan doit passer en ARCHIVE

            _mockUnitOfWork.Verify(u => u.PlanFabricationEnteteRepository.AddAsync(It.IsAny<PlanFabricationEntete>()), Times.Once);
            Assert.NotNull(savedPlan);
            Assert.Equal(4, savedPlan.Version); // Version héritée du formulaire (4)
            Assert.Equal("ACTIF", savedPlan.Statut); // Nouveau plan est toujours ACTIF
        }
    }
}
