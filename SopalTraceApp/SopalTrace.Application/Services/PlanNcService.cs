using FluentValidation;
using Microsoft.Extensions.Logging;
using SopalTrace.Application.DTOs.QualityPlans.PlansNC;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Mappers;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace SopalTrace.Application.Services;

public class PlanNcService : IPlanNcService
{
    private readonly IUnitOfWork _unitOfWork; 
    private readonly ILogger<PlanNcService> _logger;
    private readonly IPlanNcRepository _repository;
    private readonly IValidator<CreatePlanNcRequestDto> _createValidator;

    public PlanNcService(
        IUnitOfWork unitOfWork,
        IPlanNcRepository repository,
        ILogger<PlanNcService> logger,
        IValidator<CreatePlanNcRequestDto> createValidator)
    {
        _unitOfWork = unitOfWork;
        _repository = repository;
        _logger = logger;
        _createValidator = createValidator;
    }

    public async Task<Guid> CreerPlanAsync(CreatePlanNcRequestDto request, string creePar)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(_createValidator);

        var validationResult = await _createValidator.ValidateAsync(request);
        if (validationResult is null)
            throw new InvalidOperationException("Le validateur a retourné un résultat null.");

        if (!validationResult.IsValid) throw new ValidationException(validationResult.Errors);

        // 1. Archiver l'ancien plan actif s'il existe
        var planActif = await _repository.GetPlanActifAsync(request.PosteCode);
        if (planActif != null)
        {
            planActif.Statut = StatutsPlan.Archive;
            planActif.ModifiePar = creePar;
            planActif.ModifieLe = DateTime.UtcNow;
            // On commit l'archivage
            await _repository.SaveChangesAsync();
        }

        // 2. Déterminer la version
        var tousLesPlans = await _repository.GetTousLesPlansAsync();
        var maxVersion = tousLesPlans
            .Where(p => p.PosteCode == request.PosteCode)
            .Max(p => p.Version) ?? 0;

        var nouveauPlan = new PlanNcEntete
        {
            Id = Guid.NewGuid(),
            PosteCode = request.PosteCode,
            Nom = request.Nom,
            Version = maxVersion + 1,
            Statut = StatutsPlan.Actif,
            CreePar = creePar,
            CreeLe = DateTime.UtcNow,
            PlanNcLignes = new List<PlanNcLigne>()
        };

        await _repository.AddPlanAsync(nouveauPlan);
        await _repository.SaveChangesAsync();

        return nouveauPlan.Id;
    }

    public async Task<PlanNcResponseDto> GetPlanByIdAsync(Guid planId)
    {
        var plan = await _repository.GetPlanAvecRelationsAsync(planId);
        if (plan == null) throw new InvalidOperationException("Plan introuvable.");
        return PlanNcMapper.MapperEntiteVersDto(plan);
    }

    public async Task<List<PlanNcResponseDto>> GetTousLesPlansAsync()
    {
        var plans = await _repository.GetTousLesPlansAsync();
        return plans.Select(p => PlanNcMapper.MapperEntiteVersDto(p)).ToList();
    }

    public async Task<Guid> MettreAJourPlanAsync(Guid planId, SavePlanNcDto request, string modifiePar)
    {
        var planActuel = await _repository.GetPlanAvecRelationsAsync(planId);
        if (planActuel == null) throw new InvalidOperationException("Plan introuvable.");

        if (planActuel.Statut == StatutsPlan.Archive)
            throw new InvalidOperationException("Ce plan est déjà archivé.");

        // 1. Archiver l'actuel
        planActuel.Statut = StatutsPlan.Archive;
        planActuel.ModifiePar = modifiePar;
        planActuel.ModifieLe = DateTime.UtcNow;

        // 2. Créer le nouveau (V+1)
        var tousLesPlans = await _repository.GetTousLesPlansAsync();
        var maxVersion = tousLesPlans
            .Where(p => p.PosteCode == planActuel.PosteCode)
            .Max(p => p.Version) ?? 0;

        var nouveauPlan = new PlanNcEntete
        {
            Id = Guid.NewGuid(),
            PosteCode = planActuel.PosteCode,
            Nom = request.Nom,
            Version = maxVersion + 1,
            Statut = StatutsPlan.Actif,
            CreePar = modifiePar,
            CreeLe = DateTime.UtcNow,
            PlanNcLignes = request.Lignes.Select(l => new PlanNcLigne
            {
                Id = Guid.NewGuid(),
                OrdreAffiche = l.OrdreAffiche,
                MachineCode = l.MachineCode,
                RisqueDefautId = l.RisqueDefautId
            }).ToList()
        };

        await _repository.AddPlanAsync(nouveauPlan);
        await _repository.SaveChangesAsync();

        return nouveauPlan.Id;
    }

    // Le "Tree Update" simplifié (pas de sections, juste des Lignes)
    public async Task<bool> MettreAJourLignesAsync(Guid planId, List<LigneNcEditDto> LignesModifiees)
    {
        var plan = await _repository.GetPlanAvecRelationsAsync(planId);
        if (plan == null) return false;

        // 1. Supprimer les Lignes qui ne sont plus dans la liste (Liberté Totale)
        // Utilisation explicite de .Value pour éviter l'erreur de conversion implicite Guid? vers Guid
        var dtoIds = LignesModifiees
            .Select(c => c.Id)
            .OfType<Guid>()
            .ToList();

        var lignesASupprimer = plan.PlanNcLignes.Where(c => !dtoIds.Contains(c.Id)).ToList();

        foreach (var ligne in lignesASupprimer)
        {
            _repository.RemoveLigne(ligne);
        }

        // 2. Mettre à jour ou Ajouter
        foreach (var ligneDto in LignesModifiees)
        {
            var isNew = !ligneDto.Id.HasValue || ligneDto.Id.Value == Guid.Empty;

            var LigneEnBase = !isNew && ligneDto.Id is Guid ligneId
                ? plan.PlanNcLignes.FirstOrDefault(c => c.Id == ligneId)
                : null;

            if (LigneEnBase != null)
            {
                PlanNcMapper.MettreAJourLigne(LigneEnBase, ligneDto);
            }
            else
            {
                var nouvelleLigne = PlanNcMapper.ConstruireNouvelleLigne(planId, ligneDto);
                _repository.AddLigne(nouvelleLigne);
            }
        }

        // 3. Gestion des statuts et ISO 9001 (Archivage automatique de l'ancienne version)
        if (plan.Statut == StatutsPlan.Brouillon)
        {
            plan.Statut = StatutsPlan.Actif;

            var ancienPlanActif = await _repository.GetPlanActifAsync(plan.PosteCode);
            if (ancienPlanActif != null && ancienPlanActif.Id != plan.Id)
            {
                ancienPlanActif.Statut = StatutsPlan.Archive;
            }
        }

        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<Guid> CreerNouvelleVersionAsync(NouvelleVersionNcRequestDto request)
    {
        var ancienPlan = await _repository.GetPlanAvecRelationsAsync(request.AncienId);
        if (ancienPlan == null) throw new InvalidOperationException("Plan introuvable.");

        var nouveauPlan = new PlanNcEntete
        {
            Id = Guid.NewGuid(),
            PosteCode = ancienPlan.PosteCode,
            Nom = ancienPlan.Nom,
            Version = (ancienPlan.Version ?? 0) + 1,
            Statut = StatutsPlan.Brouillon,
            CreePar = request.ModifiePar,
            CreeLe = DateTime.UtcNow,
            PlanNcLignes = ancienPlan.PlanNcLignes.Select(l => new PlanNcLigne
            {
                Id = Guid.NewGuid(),
                OrdreAffiche = l.OrdreAffiche,
                MachineCode = l.MachineCode,
                RisqueDefautId = l.RisqueDefautId
            }).ToList()
        };

        await _repository.AddPlanAsync(nouveauPlan);
        await _repository.SaveChangesAsync();

        return nouveauPlan.Id;
    }

    public async Task<Guid> RestaurerPlanAsync(Guid planId, string restaurePar, string motif)
    {
        var planArchived = await _repository.GetPlanAvecRelationsAsync(planId);
        if (planArchived == null) throw new InvalidOperationException("Plan archivé introuvable.");

        // 1. Archiver le plan ACTIF actuel pour ce poste
        var planActuel = await _repository.GetPlanActifAsync(planArchived.PosteCode);
        if (planActuel != null)
        {
            planActuel.Statut = StatutsPlan.Archive;
            planActuel.ModifiePar = restaurePar;
            planActuel.ModifieLe = DateTime.UtcNow;
            // On ne sauvegarde pas tout de suite pour garder l'atomicité
        }

        // 2. Calculer la version max globale pour ce poste
        var tousLesPlans = await _repository.GetTousLesPlansAsync();
        var maxVersion = tousLesPlans
            .Where(p => p.PosteCode == planArchived.PosteCode)
            .Max(p => p.Version) ?? 0;

        // 3. Créer une nouvelle version basée sur l'archive
        var nouveauPlanId = Guid.NewGuid();
        var nouveauPlan = new PlanNcEntete
        {
            Id = nouveauPlanId,
            PosteCode = planArchived.PosteCode,
            Nom = planArchived.Nom,
            Version = maxVersion + 1,
            Statut = StatutsPlan.Actif,
            CreePar = restaurePar,
            CreeLe = DateTime.UtcNow,
            PlanNcLignes = planArchived.PlanNcLignes.Select(l => new PlanNcLigne
            {
                Id = Guid.NewGuid(),
                PlanNcenteteId = nouveauPlanId,
                OrdreAffiche = l.OrdreAffiche,
                MachineCode = l.MachineCode,
                RisqueDefautId = l.RisqueDefautId
            }).ToList()
        };

        await _repository.AddPlanAsync(nouveauPlan);
        await _repository.SaveChangesAsync();

        return nouveauPlan.Id;
    }
}
