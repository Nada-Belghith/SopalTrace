using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SopalTrace.Application.DTOs.QualityPlans.Documents;
using SopalTrace.Application.DTOs.QualityPlans.Referentiels;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Services;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;
using SopalTrace.Infrastructure.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SopalTrace.Application.Tests.Services
{
    public class DocumentServiceIntegrationTests : IDisposable
    {
        private readonly SopalTraceDbContext _context;
        private readonly DocumentService _documentService;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<ILogger<DocumentService>> _loggerMock;
        private readonly Mock<IFormulaireStructureService> _formulaireStructureServiceMock;

        public DocumentServiceIntegrationTests()
        {
            var options = new DbContextOptionsBuilder<SopalTraceDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new SopalTraceDbContext(options);
            var unitOfWork = new UnitOfWork(_context);

            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _currentUserServiceMock.Setup(c => c.UserInfo).Returns("TestUser");

            _loggerMock = new Mock<ILogger<DocumentService>>();

            var formId = Guid.NewGuid();
            _context.RefFormulaires.Add(new RefFormulaire { Id = formId, CodeReference = "FE-PF", Version = 1, Role = "PRODUIT_FINI", Designation = "FE-PF", Statut = "ACTIF" });
            _context.SaveChanges();

            _formulaireStructureServiceMock = new Mock<IFormulaireStructureService>();
            
            _formulaireStructureServiceMock
                .Setup(s => s.GetFormulaireByRoleAsync("PRODUIT_FINI"))
                .ReturnsAsync(new FormulaireStructureDto(formId, "FE-PF", "PF", null, "PRODUIT_FINI", 1));

            _documentService = new DocumentService(
                unitOfWork,
                _currentUserServiceMock.Object,
                _loggerMock.Object,
                _formulaireStructureServiceMock.Object
            );
        }

        [Fact]
        public async Task CreerDocumentAsync_DevraitEnregistrerSectionsLignesPeriodicite_EtPreserverOrdreAffiche()
        {
            // Arrange
            var typeSectionId = Guid.NewGuid();
            var regleEchantillonnageId = Guid.NewGuid();
            var periodiciteId = Guid.NewGuid();

            var request = new CreateDocumentRequestDto
            {
                TypeDocumentCode = "PLAN_PF",
                Nom = "PLAN_PF_RBGFA_SOUPAPE V1",
                Designation = "Test",
                Remarques = "NB : Pour chaque Lot, Il faut Vérifier l'aptitude à l'emploi des matériaux...",
                Sections = new List<CreateDocumentSectionDto>
                {
                    new CreateDocumentSectionDto
                    {
                        OrdreAffiche = 2,
                        LibelleSection = "Contrôle Produit Fini par échantillonnage  (2 échantillons)",
                        TypeSectionId = typeSectionId,
                        PeriodiciteId = periodiciteId,
                        RegleEchantillonnageId = regleEchantillonnageId,
                        Lignes = new List<CreateDocumentLigneDto>
                        {
                            new CreateDocumentLigneDto
                            {
                                OrdreAffiche = 1,
                                LibelleAffiche = "Essai d’endurance 500 cycles sur le filetage",
                                TypeCaracteristiqueId = Guid.NewGuid(),
                                TypeControleId = Guid.NewGuid(),
                                MoyenControleId = Guid.NewGuid(),
                                InstrumentCode = "BEE LAB : (BEE32)",
                                Observations = "Le robinet doit être à même de fonctionner sans difficulté..."
                            },
                            new CreateDocumentLigneDto
                            {
                                OrdreAffiche = 2,
                                LibelleAffiche = "Essai de résistance de queue de robinet",
                                TypeCaracteristiqueId = Guid.NewGuid(),
                                TypeControleId = Guid.NewGuid(),
                                MoyenControleId = Guid.NewGuid(),
                                InstrumentCode = "CLD",
                                Observations = "Le filetage du robinet ne doit pas être endommagé"
                            },
                            new CreateDocumentLigneDto
                            {
                                OrdreAffiche = 0, // IMPORTANT: Test the case where order might be 0, to see how backend behaves
                                LibelleAffiche = "Essai d’endurance 10000 cycles sous pression 12 bar air",
                                TypeCaracteristiqueId = Guid.NewGuid(),
                                TypeControleId = Guid.NewGuid(),
                                MoyenControleId = Guid.NewGuid(),
                                InstrumentCode = "BEE LAB : (BEE32)",
                                Observations = "Le robinet doit être à même de fonctionner..."
                            },
                            new CreateDocumentLigneDto
                            {
                                OrdreAffiche = 3,
                                LibelleAffiche = "Essai d’arrachement de la bague",
                                TypeCaracteristiqueId = Guid.NewGuid(),
                                TypeControleId = Guid.NewGuid(),
                                MoyenControleId = Guid.NewGuid(),
                                InstrumentCode = "BEE LAB : (BAN59)",
                                Observations = "Bague ni s’arracher de sa place ni se tourne"
                            }
                        }
                    }
                }
            };

            // Act
            var docId = await _documentService.CreerDocumentAsync(request);

            // Assert
            var savedDoc = await _context.DocumentEntetes
                .Include(d => d.DocumentSections)
                    .ThenInclude(s => s.DocumentLignes)
                .FirstOrDefaultAsync(d => d.Id == docId);

            Assert.NotNull(savedDoc);
            Assert.Equal("PLAN_PF", savedDoc.TypeDocumentCode);
            Assert.Single(savedDoc.DocumentSections);

            var savedSection = savedDoc.DocumentSections.First();
            Assert.Equal(2, savedSection.OrdreAffiche);
            Assert.Equal(4, savedSection.DocumentLignes.Count);
            
            // Vérification que les IDs de règle d'échantillonnage et de périodicité sont bien sauvegardés
            Assert.Equal(regleEchantillonnageId, savedSection.RegleEchantillonnageId);
            Assert.Equal(periodiciteId, savedSection.PeriodiciteId);

            // Vérification que l'OrdreAffiche correspond EXACTEMENT à ce qui a été envoyé par le Frontend (même s'il est désordonné)
            var ligne1 = savedSection.DocumentLignes.FirstOrDefault(l => l.LibelleAffiche == "Essai d’endurance 500 cycles sur le filetage");
            Assert.NotNull(ligne1);
            Assert.Equal(1, ligne1.OrdreAffiche);

            var ligne2 = savedSection.DocumentLignes.FirstOrDefault(l => l.LibelleAffiche == "Essai de résistance de queue de robinet");
            Assert.NotNull(ligne2);
            Assert.Equal(2, ligne2.OrdreAffiche);

            var ligne0 = savedSection.DocumentLignes.FirstOrDefault(l => l.LibelleAffiche == "Essai d’endurance 10000 cycles sous pression 12 bar air");
            Assert.NotNull(ligne0);
            Assert.Equal(0, ligne0.OrdreAffiche); // Le backend sauvegarde le 0 exactement tel quel !

            var ligne3 = savedSection.DocumentLignes.FirstOrDefault(l => l.LibelleAffiche == "Essai d’arrachement de la bague");
            Assert.NotNull(ligne3);
            Assert.Equal(3, ligne3.OrdreAffiche);
            
            // Simulation de la requête de lecture avec OrderBy (comme le fait l'API de consultation ou le Frontend)
            var orderedLines = savedSection.DocumentLignes.OrderBy(l => l.OrdreAffiche).ToList();
            Assert.Equal("Essai d’endurance 10000 cycles sous pression 12 bar air", orderedLines[0].LibelleAffiche); // Ordre 0 s'affiche en premier !
            Assert.Equal("Essai d’endurance 500 cycles sur le filetage", orderedLines[1].LibelleAffiche); // Ordre 1 s'affiche en deuxième !
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
