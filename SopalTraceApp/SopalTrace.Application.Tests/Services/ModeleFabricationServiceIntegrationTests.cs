using Microsoft.EntityFrameworkCore;
using Moq;
using SopalTrace.Application.DTOs.QualityPlans.Documents;
using SopalTrace.Application.DTOs.QualityPlans.Modeles;
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
    public class ModeleFabricationServiceIntegrationTests : IDisposable
    {
        private readonly SopalTraceDbContext _context;
        private readonly ModeleFabricationService _modeleFabricationService;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<IFormulaireStructureService> _formulaireStructureServiceMock;

        public ModeleFabricationServiceIntegrationTests()
        {
            var options = new DbContextOptionsBuilder<SopalTraceDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new SopalTraceDbContext(options);

            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _currentUserServiceMock.Setup(c => c.UserInfo).Returns("TestUser");

            var formId = Guid.NewGuid();
            _context.RefFormulaires.Add(new RefFormulaire { Id = formId, CodeReference = "FE-PRC", Version = 1, Role = "EN_COURS_DE_FABRICATION", Designation = "FE-PRC", Statut = "ACTIF" });
            _context.SaveChanges();

            _formulaireStructureServiceMock = new Mock<IFormulaireStructureService>();
            
            _formulaireStructureServiceMock
                .Setup(s => s.GetFormulaireByRoleAsync("EN_COURS_DE_FABRICATION"))
                .ReturnsAsync(new FormulaireStructureDto(formId, "FE-PRC", "PRC", null, "EN_COURS_DE_FABRICATION", 1));

            var unitOfWork = new UnitOfWork(_context);

            _modeleFabricationService = new ModeleFabricationService(
                unitOfWork,
                _currentUserServiceMock.Object,
                _formulaireStructureServiceMock.Object
            );
        }

        [Fact]
        public async Task CreerNouvelleVersionModele_DoitArchiverLancien_EtCreerNouveau_AvecMemeVersion()
        {
            // ==========================================
            // ARRANGE (Préparation des données en base)
            // ==========================================
            string codeArticle = "MOD_TEST_001";
            
            var createReq = new CreateModeleRequestDto
            {
                Code = codeArticle,
                Libelle = "Modele Test",
                TypeRobinetCode = "ROB1",
                NatureComposantCode = "NAT",
                FamilleProduitCode = "FAM",
                OperationCode = "OP1"
            };

            Guid premierModeleId = await _modeleFabricationService.CreerModeleAsync(createReq);

            var premierModeleEnBase = await _context.ModeleFabricationEntetes.FindAsync(premierModeleId);
            Assert.NotNull(premierModeleEnBase);
            Assert.Equal("ACTIF", premierModeleEnBase.Statut);

            // ==========================================
            // ACT (Exécution du métier à tester)
            // ==========================================
            var newVersionReq = new NouvelleVersionModeleRequestDto
            {
                AncienId = premierModeleId,
                Code = codeArticle
            };

            Guid deuxiemeModeleId = await _modeleFabricationService.CreerNouvelleVersionModeleAsync(newVersionReq);

            // ==========================================
            // ASSERT (Vérification des résultats attendus)
            // ==========================================
            var ancienModele = await _context.ModeleFabricationEntetes.FindAsync(premierModeleId);
            var nouveauModele = await _context.ModeleFabricationEntetes.FindAsync(deuxiemeModeleId);

            Assert.NotNull(ancienModele);
            Assert.NotNull(nouveauModele);

            Assert.NotEqual(ancienModele.Id, nouveauModele.Id);

            Assert.Equal("ARCHIVE", ancienModele.Statut);
            Assert.Equal("ACTIF", nouveauModele.Statut);

            // Assert sur l'incrémentation du libellé (.0 puis .1)
            Assert.EndsWith(".0", ancienModele.Libelle);
            Assert.EndsWith(".1", nouveauModele.Libelle);

            Assert.Equal(1, ancienModele.Version);
            Assert.Equal(1, nouveauModele.Version);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
