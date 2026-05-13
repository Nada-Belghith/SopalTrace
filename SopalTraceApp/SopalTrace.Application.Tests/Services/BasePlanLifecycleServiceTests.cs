using Xunit;
using Moq;
using SopalTrace.Application.Services;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Tests.Services;

/// <summary>
/// EXEMPLE: Tests unitaires pour BasePlanLifecycleService avec ses Hooks.
/// 
/// Montre comment:
/// - Tester le Template Method Pattern
/// - Tester les hooks (virtual methods)
/// - Mocker les abstracts
/// - Valider le cycle de vie complet
/// </summary>
public class BasePlanLifecycleServiceTests
{
    // ==================== SETUP ====================

    /// <summary>
    /// Implémentation test de BasePlanLifecycleService
    /// pour tester la logique de base.
    /// </summary>
    private class TestPlanService : BasePlanLifecycleService<TestPlanEntete, TestCreateDto, TestUpdateDto>
    {
        private Dictionary<Guid, TestPlanEntete> _store = new();
        private List<string> _hooksCalled = new();

        public List<string> HooksCalled => _hooksCalled;

        public TestPlanService(IUnitOfWork unitOfWork) : base(unitOfWork)
        {
        }

        // Abstracts
        protected override Task<TestPlanEntete?> ObtenirBrouillonExistantAsync(TestCreateDto dto)
        {
            return Task.FromResult<TestPlanEntete?>(null);
        }

        protected override async Task<TestPlanEntete?> ObtenirEntiteAsync(Guid id)
        {
            _store.TryGetValue(id, out var result);
            return await Task.FromResult(result);
        }

        protected override async Task<TestPlanEntete> CreerEntiteAsync(TestCreateDto dto, string user)
        {
            var plan = new TestPlanEntete
            {
                Id = Guid.NewGuid(),
                Designation = dto.Designation,
                CreePar = user,
                CreeLe = DateTime.UtcNow
            };
            _store[plan.Id] = plan;
            return await Task.FromResult(plan);
        }

        protected override async Task ApplierMiseAJourDraftAsync(TestPlanEntete plan, TestUpdateDto dto, string user)
        {
            plan.Designation = dto.Designation ?? plan.Designation;
            plan.ModifiePar = user;
            plan.ModifieLe = DateTime.UtcNow;
            await Task.CompletedTask;
        }

        protected override async Task PersisterEntiteAsync(TestPlanEntete plan)
        {
            _store[plan.Id] = plan;
            await Task.CompletedTask;
        }

        protected override async Task<int> CalculerNouvelleVersionAsync(TestPlanEntete plan)
        {
            return await Task.FromResult(plan.Version + 1);
        }

        protected override async Task<TestPlanEntete> CreerNouvelleVersionEntiteAsync(
            TestPlanEntete ancienPlan,
            TestUpdateDto dto,
            int nouvelleVersion,
            string user)
        {
            var nouveauPlan = new TestPlanEntete
            {
                Id = Guid.NewGuid(),
                Designation = dto.Designation ?? ancienPlan.Designation,
                Version = nouvelleVersion,
                CreePar = user,
                CreeLe = DateTime.UtcNow
            };
            _store[nouveauPlan.Id] = nouveauPlan;
            return await Task.FromResult(nouveauPlan);
        }

        // Hooks
        protected override async Task<List<string>> ValidateCreationAsync(TestCreateDto dto)
        {
            _hooksCalled.Add("ValidateCreationAsync");
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(dto.Designation))
                errors.Add("Designation requise");
            return await Task.FromResult(errors);
        }

        protected override async Task HandleVersioningBeforeActivationAsync(TestPlanEntete plan, string user)
        {
            _hooksCalled.Add("HandleVersioningBeforeActivationAsync");
            await Task.CompletedTask;
        }

        protected override async Task OnPlanActivatedAsync(TestPlanEntete activatedPlan, string user)
        {
            _hooksCalled.Add("OnPlanActivatedAsync");
            await Task.CompletedTask;
        }
    }

    // ==================== TEST CLASSES ====================

    public class TestPlanEntete : IPlanEntete
    {
        public Guid Id { get; set; }
        public string Statut { get; set; } = StatutsPlan.Brouillon;
        public int Version { get; set; } = 1;
        public string? ModifiePar { get; set; }
        public DateTime? ModifieLe { get; set; }
        public string Designation { get; set; } = string.Empty;
        public string CreePar { get; set; } = string.Empty;
        public DateTime CreeLe { get; set; } = DateTime.UtcNow;
    }

    public class TestCreateDto
    {
        public string? Designation { get; set; }
    }

    public class TestUpdateDto
    {
        public string? Designation { get; set; }
    }

    // ==================== TESTS ====================

    [Fact]
    public async Task CreerBrouillonAsync_ShouldCreatePlanWithBrouillonStatus()
    {
        // Arrange
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var service = new TestPlanService(mockUnitOfWork.Object);
        var dto = new TestCreateDto { Designation = "Test Plan" };

        // Act
        var planId = await service.CreerBrouillonAsync(dto, "USER1");

        // Assert
        var plan = await service.ConsulterPlanAsync(planId);
        Assert.NotNull(plan);
        Assert.Equal("BROUILLON", plan.Statut);
        Assert.Equal(1, plan.Version);
        Assert.Equal("Test Plan", plan.Designation);
        Assert.True(service.HooksCalled.Contains("ValidateCreationAsync"));
    }

    [Fact]
    public async Task CreerBrouillonAsync_WithInvalidData_ShouldThrow()
    {
        // Arrange
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var service = new TestPlanService(mockUnitOfWork.Object);
        var dto = new TestCreateDto { Designation = null }; // Invalid

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreerBrouillonAsync(dto, "USER1")
        );
    }

    [Fact]
    public async Task UpdateDraftAsync_ShouldUpdatePlanAndKeepBrouillonStatus()
    {
        // Arrange
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var service = new TestPlanService(mockUnitOfWork.Object);
        var planId = await service.CreerBrouillonAsync(new TestCreateDto { Designation = "Initial" }, "USER1");

        // Act
        await service.UpdateDraftAsync(planId, new TestUpdateDto { Designation = "Updated" }, "USER2");

        // Assert
        var plan = await service.ConsulterPlanAsync(planId);
        Assert.Equal("Updated", plan.Designation);
        Assert.Equal("BROUILLON", plan.Statut); // Toujours brouillon
        Assert.Equal("USER2", plan.ModifiePar);
    }

    [Fact]
    public async Task ActiverPlanAsync_ShouldTransitionToActifAndCallHooks()
    {
        // Arrange
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var service = new TestPlanService(mockUnitOfWork.Object);
        var planId = await service.CreerBrouillonAsync(new TestCreateDto { Designation = "To Activate" }, "USER1");

        // Clear hooks from creation
        service.HooksCalled.Clear();

        // Act
        await service.ActiverPlanAsync(planId, "USER1");

        // Assert
        var plan = await service.ConsulterPlanAsync(planId);
        Assert.Equal("ACTIF", plan.Statut);
        Assert.Equal(2, plan.Version); // Version incrémentée
        Assert.True(service.HooksCalled.Contains("HandleVersioningBeforeActivationAsync"));
        Assert.True(service.HooksCalled.Contains("OnPlanActivatedAsync"));
    }

    [Fact]
    public async Task ActiverPlanAsync_WithNonDraftPlan_ShouldThrow()
    {
        // Arrange
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var service = new TestPlanService(mockUnitOfWork.Object);
        var planId = await service.CreerBrouillonAsync(new TestCreateDto { Designation = "Test" }, "USER1");
        await service.ActiverPlanAsync(planId, "USER1");

        // Act & Assert - Tentative d'activation d'un plan déjà actif
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ActiverPlanAsync(planId, "USER1")
        );
    }

    [Fact]
    public async Task ArchiverPlanAsync_ShouldTransitionToArchive()
    {
        // Arrange
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var service = new TestPlanService(mockUnitOfWork.Object);
        var planId = await service.CreerBrouillonAsync(new TestCreateDto { Designation = "To Archive" }, "USER1");
        await service.ActiverPlanAsync(planId, "USER1");

        // Act
        await service.ArchiverPlanAsync(planId, "ARCHIVER");

        // Assert
        var plan = await service.ConsulterPlanAsync(planId);
        Assert.Equal("ARCHIVE", plan.Statut);
        Assert.Equal("ARCHIVER", plan.ModifiePar);
    }

    [Fact]
    public async Task SecuriserNomAuteur_ShouldLimitTo50Chars()
    {
        // Arrange
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var service = new TestPlanService(mockUnitOfWork.Object);

        // Act
        var planId = await service.CreerBrouillonAsync(
            new TestCreateDto { Designation = "Test" },
            "VERY_LONG_USERNAME_THAT_EXCEEDS_FIFTY_CHARACTERS_BY_A_LOT_MORE"
        );

        // Assert
        var plan = await service.ConsulterPlanAsync(planId);
        Assert.True(plan.CreePar.Length <= 50);
    }

    [Fact]
    public async Task SecuriserNomAuteur_ShouldDefaultToSystem()
    {
        // Arrange
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var service = new TestPlanService(mockUnitOfWork.Object);

        // Act
        var planId = await service.CreerBrouillonAsync(
            new TestCreateDto { Designation = "Test" },
            null! // null ou empty
        );

        // Assert
        var plan = await service.ConsulterPlanAsync(planId);
        Assert.Equal("SYSTEM", plan.CreePar);
    }

    [Fact]
    public async Task CompleteLifecycle_ShouldWorkCorrectly()
    {
        // Arrange
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var service = new TestPlanService(mockUnitOfWork.Object);

        // Act: Créer
        var planId = await service.CreerBrouillonAsync(
            new TestCreateDto { Designation = "Lifecycle Test" },
            "CREATOR"
        );
        Assert.Equal("BROUILLON", (await service.ConsulterPlanAsync(planId)).Statut);

        // Act: Mettre à jour
        await service.UpdateDraftAsync(
            planId,
            new TestUpdateDto { Designation = "Lifecycle Test V2" },
            "UPDATER"
        );
        Assert.Equal("Lifecycle Test V2", (await service.ConsulterPlanAsync(planId)).Designation);

        // Act: Activer
        await service.ActiverPlanAsync(planId, "ACTIVATOR");
        Assert.Equal("ACTIF", (await service.ConsulterPlanAsync(planId)).Statut);
        Assert.Equal(2, (await service.ConsulterPlanAsync(planId)).Version);

        // Act: Archiver
        await service.ArchiverPlanAsync(planId, "ARCHIVER");
        Assert.Equal("ARCHIVE", (await service.ConsulterPlanAsync(planId)).Statut);

        // Assert: Tous les hooks ont été appelés
        Assert.True(service.HooksCalled.Contains("ValidateCreationAsync"));
        Assert.True(service.HooksCalled.Contains("HandleVersioningBeforeActivationAsync"));
        Assert.True(service.HooksCalled.Contains("OnPlanActivatedAsync"));
    }
}

/// <summary>
/// Tests spécifiques pour BasePlanArticleLifecycleService.
/// </summary>
public class BasePlanArticleLifecycleServiceTests
{
    // Tests similaires mais pour les services par article
    // Ex: SauvegardeAutoAsync, RestaurerPlanArchiveAsync, etc.

    [Fact]
    public async Task SauvegardeAutoAsync_ShouldUpdateAutoSaveTime()
    {
        // Arrange
        // ...

        // Act
        // var autoSaveTime = await service.SauvegardeAutoAsync(...);

        // Assert
        // plan.DateDerniereSauvegardeAuto should be updated
    }
}
