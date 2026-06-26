using Microsoft.EntityFrameworkCore;
using Moq;
using SopalTrace.Application.DTOs.QualityPlans.Fabrication;
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
    public class PlanFabricationServiceIntegrationTests : IDisposable
    {
        private readonly SopalTraceDbContext _context;
        private readonly PlanFabricationService _planFabricationService;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<IFormulaireStructureService> _formulaireStructureServiceMock;

        public PlanFabricationServiceIntegrationTests()
        {
            // 1. Configuration de la base de données "In-Memory"
            var options = new DbContextOptionsBuilder<SopalTraceDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Une BDD unique par test
                .Options;

            _context = new SopalTraceDbContext(options);
            var unitOfWork = new UnitOfWork(_context);

            // 2. Mock des services annexes (pour simuler l'utilisateur et le formulaire PRC)
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _currentUserServiceMock.Setup(c => c.UserInfo).Returns("TestUser");

            var formId = Guid.NewGuid();
            _context.RefFormulaires.Add(new RefFormulaire { Id = formId, CodeReference = "FE-PRC", Version = 1, Role = "EN_COURS_DE_FABRICATION", Designation = "FE-PRC", Statut = "ACTIF" });
            _context.SaveChanges();

            _formulaireStructureServiceMock = new Mock<IFormulaireStructureService>();
            
            // Simuler un formulaire PRC existant en version 1
            _formulaireStructureServiceMock
                .Setup(s => s.GetFormulaireByRoleAsync("EN_COURS_DE_FABRICATION"))
                .ReturnsAsync(new FormulaireStructureDto(formId, "FE-PRC", "PRC", null, "EN_COURS_DE_FABRICATION", 1));

            // 3. Instanciation du vrai service à tester
            var frequencyParserServiceMock = new Mock<IFrequencyParserService>();

            _planFabricationService = new PlanFabricationService(
                unitOfWork,
                _currentUserServiceMock.Object,
                _formulaireStructureServiceMock.Object,
                frequencyParserServiceMock.Object
            );
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task CreerNouvelleVersionPlan_DoitArchiverLancien_EtCreerNouveau_AvecMemeVersion()
        {
            // ==========================================
            // ARRANGE (Préparation des données)
            // ==========================================
            string codeArticle = "ART-TEST-001";

            var createReq = new CreatePlanFabricationRequestDto
            {
                Nom = codeArticle,
                Designation = "Plan de test unitaire",
                OperationCode = "OP1",
                VersionInitiale = 1
            };

            // Création du premier plan
            Guid premierPlanId = await _planFabricationService.CreerPlanAsync(createReq);

            // Vérification intermédiaire
            var premierPlanEnBase = await _context.PlanFabricationEntetes.FindAsync(premierPlanId);
            Assert.NotNull(premierPlanEnBase);
            Assert.Equal("ACTIF", premierPlanEnBase.Statut);

            // ==========================================
            // ACT (Exécution du métier à tester)
            // ==========================================
            var newVersionReq = new NouvelleVersionPlanFabricationRequestDto
            {
                AncienId = premierPlanId,
                Nom = codeArticle,
                OperationCode = "OP1"
            };
            var deuxiemePlanId = await _planFabricationService.CreerNouvelleVersionPlanAsync(newVersionReq);

            // ==========================================
            // ASSERT (Vérification des résultats attendus)
            // ==========================================
            
            // Recharger les entités depuis la base In-Memory
            var ancienPlan = await _context.PlanFabricationEntetes.FindAsync(premierPlanId);
            var nouveauPlan = await _context.PlanFabricationEntetes.FindAsync(deuxiemePlanId);

            Assert.NotNull(ancienPlan);
            Assert.NotNull(nouveauPlan);

            // 1. Règle métier : l'ancien doit être archivé
            Assert.Equal("ARCHIVE", ancienPlan.Statut);
            Assert.Equal("ACTIF", nouveauPlan.Statut);

            // Assert sur l'incrémentation du Nom (.0 puis .1)
            Assert.EndsWith(".0", ancienPlan.Nom);
            Assert.EndsWith(".1", nouveauPlan.Nom);

            // 3. Règle métier : puisqu'on a simulé que le PRC est toujours en V1, le nouveau plan doit garder la version 1
            Assert.Equal(1, ancienPlan.Version);
            Assert.Equal(1, nouveauPlan.Version);
            
            // 4. Les Ids doivent être différents
            Assert.NotEqual(ancienPlan.Id, nouveauPlan.Id);
        }
    }
}
