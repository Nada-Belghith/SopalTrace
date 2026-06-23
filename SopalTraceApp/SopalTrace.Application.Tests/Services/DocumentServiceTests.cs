using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SopalTrace.Application.DTOs.QualityPlans.Documents;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Services;
using SopalTrace.Domain.Entities;
using Xunit;

namespace SopalTrace.Application.Tests.Services
{
    public class DocumentServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<ILogger<DocumentService>> _mockLogger;
        private readonly Mock<IFormulaireStructureService> _mockFormulaireStructureService;
        
        private readonly DocumentService _documentService;

        public DocumentServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockLogger = new Mock<ILogger<DocumentService>>();
            _mockFormulaireStructureService = new Mock<IFormulaireStructureService>();

            // Setup current user
            _mockCurrentUserService.Setup(c => c.UserInfo)
                .Returns("TestUser");

            // Init service
            _documentService = new DocumentService(
                _mockUnitOfWork.Object,
                _mockCurrentUserService.Object,
                _mockLogger.Object,
                _mockFormulaireStructureService.Object
            );
        }

        [Fact]
        public async Task CreerNouvelleVersion_DevraitArchiverAncienDocument_SiActif()
        {
            // Arrange
            var ancienId = Guid.NewGuid();
            var ancienDoc = new DocumentEntete
            {
                Id = ancienId,
                Statut = "ACTIF",
                TypeDocumentCode = "RESULTAT_CF",
                Nom = "Test Doc V1",
                Version = 1,
                DocumentSections = new List<DocumentSection>()
            };

            var request = new NouvelleVersionDocumentRequestDto
            {
                AncienId = ancienId,
                TypeDocumentCode = "RESULTAT_CF",
                Nom = "Test Doc",
                Sections = new List<CreateDocumentSectionDto>()
            };

            // Mocks Setup
            _mockUnitOfWork.Setup(u => u.DocumentEnteteRepository.GetByIdAsync(ancienId, true))
                .ReturnsAsync(ancienDoc);

            _mockUnitOfWork.Setup(u => u.DocumentEnteteRepository.GetLatestVersionAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(1); // latest version is 1

            // Setup void methods
            _mockUnitOfWork.Setup(u => u.DocumentEnteteRepository.UpdateAsync(It.IsAny<DocumentEntete>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.DocumentEnteteRepository.AddAsync(It.IsAny<DocumentEntete>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _documentService.CreerNouvelleVersionDocumentAsync(request);

            // Assert
            // 1. L'ancien document doit passer en ARCHIVE
            Assert.Equal("ARCHIVE", ancienDoc.Statut);
            _mockUnitOfWork.Verify(u => u.DocumentEnteteRepository.UpdateAsync(ancienDoc), Times.Once);

            // 2. Le nouveau document doit être ajouté et avoir la version 2
            _mockUnitOfWork.Verify(u => u.DocumentEnteteRepository.AddAsync(It.Is<DocumentEntete>(d => 
                d.Statut == "ACTIF" && d.Version == 2)), Times.Once);

            // 3. Commit a bien été appelé
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task CreerNouvelleVersion_NeDoitPasArchiver_SiDejaBrouillon()
        {
            // Arrange
            var ancienId = Guid.NewGuid();
            var ancienDoc = new DocumentEntete
            {
                Id = ancienId,
                Statut = "BROUILLON", // Déjà brouillon
                TypeDocumentCode = "RESULTAT_CF",
                Nom = "Test Doc",
                Version = 1,
                DocumentSections = new List<DocumentSection>()
            };

            var request = new NouvelleVersionDocumentRequestDto
            {
                AncienId = ancienId,
                TypeDocumentCode = "RESULTAT_CF",
                Nom = "Test Doc",
                Sections = new List<CreateDocumentSectionDto>()
            };

            _mockUnitOfWork.Setup(u => u.DocumentEnteteRepository.GetByIdAsync(ancienId, true))
                .ReturnsAsync(ancienDoc);

            _mockUnitOfWork.Setup(u => u.DocumentEnteteRepository.GetLatestVersionAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(1); 

            // Act
            await _documentService.CreerNouvelleVersionDocumentAsync(request);

            // Assert
            // Ne doit pas modifier l'ancien document s'il n'est pas actif
            _mockUnitOfWork.Verify(u => u.DocumentEnteteRepository.UpdateAsync(ancienDoc), Times.Never);
        }
    }
}
