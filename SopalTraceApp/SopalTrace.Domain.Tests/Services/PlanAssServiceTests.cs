//using FluentValidation;
//using FluentValidation.Results;
//using Moq;
//using SopalTrace.Application.DTOs.QualityPlans.PlanAssemblage;
//using SopalTrace.Application.Interfaces;
//using SopalTrace.Application.Services;
//using SopalTrace.Domain.Constants;
//using SopalTrace.Domain.Entities;
//using SopalTrace.Domain.Exceptions;
//using Xunit;
//using Microsoft.Extensions.Logging;
//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace SopalTrace.Domain.Tests.Services;

/////// <summary>
/////// Tests unitaires pour le service PlanAssService
/////// </summary>
////public class PlanAssServiceTests
////{
////    private readonly Mock<IPlanAssRepository> _mockRepository;
////    private readonly Mock<IValidator<CreatePlanAssRequestDto>> _mockValidator;
////    private readonly Mock<ILogger<PlanAssService>> _mockLogger;
////    private readonly PlanAssService _service;

////    public PlanAssServiceTests()
////    {
////        _mockRepository = new Mock<IPlanAssRepository>();
////        _mockValidator = new Mock<IValidator<CreatePlanAssRequestDto>>();
////        _mockLogger = new Mock<ILogger<PlanAssService>>();
////        _service = new PlanAssService(_mockRepository.Object, _mockValidator.Object, _mockLogger.Object);
////    }

////    [Fact]
////    public async Task CreerPlanAsync_WithValidPlanMaitre_ReturnsGuid()
////    {
////        // Arrange
////        var request = new CreatePlanAssRequestDto
////        {
////            OperationCode = "OP001",
////            TypeRobinetCode = "TR001",
////            EstModele = true,
////            Nom = "Plan Maître Test"
////        };

////        var validationResult = new ValidationResult();
////        _mockValidator.Setup(v => v.ValidateAsync(request, default))
////            .ReturnsAsync(validationResult);

////        _mockRepository.Setup(r => r.ExistePlanMaitreActifAsync("OP001", "TR001"))
////            .ReturnsAsync(false);

////        _mockRepository.Setup(r => r.GetDerniereVersionAsync("OP001", "TR001", null))
////            .ReturnsAsync(0);

////        _mockRepository.Setup(r => r.SaveChangesAsync())
////            .Returns(Task.CompletedTask);

////        // Act
////        var result = await _service.CreerPlanAsync(request, "TestUser");

////        // Assert
////        Assert.NotEqual(Guid.Empty, result);
////        _mockRepository.Verify(r => r.AddPlanAsync(It.IsAny<PlanAssEntete>()), Moq.Times.Once);
////        _mockRepository.Verify(r => r.SaveChangesAsync(), Moq.Times.Once);
////    }

////    [Fact]
////    public async Task CreerPlanAsync_WithExistingActivePlanMaitre_ThrowsException()
////    {
////        // Arrange
////        var request = new CreatePlanAssRequestDto
////        {
////            OperationCode = "OP001",
////            TypeRobinetCode = "TR001",
////            EstModele = true,
////            Nom = "Plan Maître Test"
////        };

////        var validationResult = new ValidationResult();
////        _mockValidator.Setup(v => v.ValidateAsync(request, default))
////            .ReturnsAsync(validationResult);

////        _mockRepository.Setup(r => r.ExistePlanMaitreActifAsync("OP001", "TR001"))
////            .ReturnsAsync(true);

////        // Act & Assert
////        await Assert.ThrowsAsync<PlanMaitreAlreadyExistsException>(
////            () => _service.CreerPlanAsync(request, "TestUser"));
////    }

////    [Fact]
////    public async Task CreerPlanAsync_WithMissingArticleCodeForException_ThrowsException()
////    {
////        // Arrange
////        var request = new CreatePlanAssRequestDto
////        {
////            OperationCode = "OP001",
////            TypeRobinetCode = "TR001",
////            EstModele = false,
////            CodeArticleSage = null,
////            Nom = "Plan Exception Test"
////        };

////        var validationResult = new ValidationResult();
////        _mockValidator.Setup(v => v.ValidateAsync(request, default))
////            .ReturnsAsync(validationResult);

////        // Act & Assert
////        await Assert.ThrowsAsync<MissingArticleCodeException>(
////            () => _service.CreerPlanAsync(request, "TestUser"));
////    }

////    [Fact]
////    public async Task GetPlanByIdAsync_WithNonExistentPlan_ThrowsNotFoundException()
////    {
////        // Arrange
////        var planId = Guid.NewGuid();
////        _mockRepository.Setup(r => r.GetPlanAvecRelationsAsync(planId))
////            .ReturnsAsync((PlanAssEntete?)null);

////        // Act & Assert
////        await Assert.ThrowsAsync<PlanNotFoundException>(
////            () => _service.GetPlanByIdAsync(planId));
////    }

////    [Fact]
////    public async Task GetPlanByIdAsync_WithExistentPlan_ReturnsPlanDto()
////    {
////        // Arrange
////        var planId = Guid.NewGuid();
////        var plan = new PlanAssEntete
////        {
////            Id = planId,
////            OperationCode = "OP001",
////            TypeRobinetCode = "TR001",
////            EstModele = true,
////            Nom = "Test Plan",
////            Version = 1,
////            Statut = StatutsPlan.Actif,
////            CreePar = "TestUser",
////            CreeLe = DateTime.UtcNow,
////            PlanAssSections = new List<PlanAssSection>()
////        };

////        _mockRepository.Setup(r => r.GetPlanAvecRelationsAsync(planId))
////            .ReturnsAsync(plan);

////        // Act
////        var result = await _service.GetPlanByIdAsync(planId);

////        // Assert
////        Assert.NotNull(result);
////        Assert.Equal(planId, result.Id);
////        Assert.Equal("Test Plan", result.Nom);
////    }

////    [Fact]
////    public async Task ChangerStatutPlanAsync_WithValidPlan_ChangesStatus()
////    {
////        // Arrange
////        var planId = Guid.NewGuid();
////        var plan = new PlanAssEntete
////        {
////            Id = planId,
////            OperationCode = "OP001",
////            TypeRobinetCode = "TR001",
////            EstModele = true,
////            Nom = "Test Plan",
////            Version = 1,
////            Statut = StatutsPlan.Brouillon,
////            CreePar = "TestUser",
////            CreeLe = DateTime.UtcNow,
////            PlanAssSections = new List<PlanAssSection>()
////        };

////        var statusRequest = new ChangePlanAssStatusRequestDto
////        {
////            NouveauStatut = StatutsPlan.Actif,
////            Motif = "Test"
////        };

////        _mockRepository.Setup(r => r.GetPlanAvecRelationsAsync(planId))
////            .ReturnsAsync(plan);

////        _mockRepository.Setup(r => r.SaveChangesAsync())
////            .Returns(Task.CompletedTask);

////        // Act
////        var result = await _service.ChangerStatutPlanAsync(planId, statusRequest, "ModifierUser");

////        // Assert
////        Assert.True(result);
////        Assert.Equal(StatutsPlan.Actif, plan.Statut);
////        _mockRepository.Verify(r => r.SaveChangesAsync(), Moq.Times.Once);
////    }
////}