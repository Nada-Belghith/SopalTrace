using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SopalTrace.Application.DTOs.QualityPlans.PlanVerifMachines;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Services;
using SopalTrace.Domain.Entities;
using Xunit;

namespace SopalTrace.Application.Tests.Services
{
    public class PlanVerifMachineServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<ILogger<PlanVerifMachineService>> _mockLogger;
        private readonly Mock<IFormulaireStructureService> _mockFormulaireStructureService;

        private readonly PlanVerifMachineService _service;

        public PlanVerifMachineServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockLogger = new Mock<ILogger<PlanVerifMachineService>>();
            _mockFormulaireStructureService = new Mock<IFormulaireStructureService>();

            _mockCurrentUserService.Setup(c => c.UserInfo)
                .Returns("TestUser");

            _service = new PlanVerifMachineService(
                _mockUnitOfWork.Object,
                _mockCurrentUserService.Object,
                _mockLogger.Object,
                _mockFormulaireStructureService.Object
            );
        }

        [Fact]
        public async Task MettreAJourPlanVerifMachineAsync_DoitSupprimerAncienEtAjouterNouveau()
        {
            // Arrange
            var planId = Guid.NewGuid();
            var existingDoc = new PlanVerifMachineEntete
            {
                Id = planId,
                MachineCode = "MAC-01",
                Nom = "Plan VM MAC-01 - V1",
                Version = 1,
                Statut = "BROUILLON"
            };

            var request = new UpdatePlanVerifMachineRequestDto
            {
                MachineCode = "MAC-01",
                Nom = "Plan VM MAC-01"
            };

            _mockUnitOfWork.Setup(u => u.PlanVerifMachineEnteteRepository.GetByIdAsync(planId, false))
                .ReturnsAsync(existingDoc); 
            _mockUnitOfWork.Setup(u => u.PlanVerifMachineEnteteRepository.GetByIdAsync(planId, true))
                .ReturnsAsync(existingDoc); 

            _mockUnitOfWork.Setup(u => u.PlanVerifMachineEnteteRepository.DeleteAsync(It.IsAny<PlanVerifMachineEntete>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.PlanVerifMachineEnteteRepository.AddAsync(It.IsAny<PlanVerifMachineEntete>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitAsync())
                .ReturnsAsync(1);

            // Act
            await _service.MettreAJourPlanVerifMachineAsync(planId, request);

            // Assert
            _mockUnitOfWork.Verify(u => u.PlanVerifMachineEnteteRepository.DeleteAsync(existingDoc), Times.Once);

            _mockUnitOfWork.Verify(u => u.PlanVerifMachineEnteteRepository.AddAsync(It.Is<PlanVerifMachineEntete>(d => 
                d.MachineCode == "MAC-01" && 
                d.Statut == "BROUILLON" && 
                d.Version == 1)), Times.Once);

            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task CreerPlanVerifMachineAsync_DoitPrendreVersionDuFormulaire_SiExistant()
        {
            // Arrange
            var request = new CreatePlanVerifMachineRequestDto
            {
                MachineCode = "MAC-02",
                Nom = "Plan VM MAC-02",
                RefFormulaireCodeReference = "REF-MAC-02"
            };

            var formResultId = Guid.NewGuid();
            var formResult = new RefFormulaire 
            { 
                Id = formResultId, 
                CodeReference = "REF-MAC-02",
                Version = 3, 
                Statut = "ACTIF"
            };

            _mockUnitOfWork.Setup(u => u.PlanVerifMachineEnteteRepository.GetByMachineCodeAsync("MAC-02"))
                .ReturnsAsync(new List<PlanVerifMachineEntete>()); 

            _mockUnitOfWork.Setup(u => u.RefFormulaireRepository.GetFormulaireActifByCodeReferenceAsync("REF-MAC-02"))
                .ReturnsAsync(formResult);

            _mockFormulaireStructureService.Setup(s => s.UpdateFormulaireStructureAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>()))
                .ReturnsAsync(new (Guid Id, int Version)?((formResultId, 3)));

            _mockUnitOfWork.Setup(u => u.RefFormulaireRepository.GetByIdAsync(formResultId))
                .ReturnsAsync(formResult); 

            _mockUnitOfWork.Setup(u => u.PlanVerifMachineEnteteRepository.AddAsync(It.IsAny<PlanVerifMachineEntete>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.CreerPlanVerifMachineAsync(request);

            // Assert
            _mockUnitOfWork.Verify(u => u.PlanVerifMachineEnteteRepository.AddAsync(It.Is<PlanVerifMachineEntete>(d => 
                d.FormulaireId == formResultId &&
                d.Version == 3 && 
                d.Statut == "ACTIF" 
            )), Times.Once);
        }
    }
}
