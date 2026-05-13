using FluentValidation;
using Microsoft.Extensions.Logging;
using SopalTrace.Application.DTOs.QualityPlans.PlansNC;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Mappers;
using SopalTrace.Application.Utilities;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Application.Services;

/// <summary>
/// Service de gestion des Plans NC.
/// Hérite de BasePlanLifecycleService pour centralize le cycle de vie (CRUD, versionning, archivage).
/// Implémente les hooks spécifiques à NC.
/// </summary>
public class PlanNcService : BasePlanLifecycleService<PlanNcEntete, CreatePlanNcRequestDto, SavePlanNcDto>, IPlanNcService
{
    private readonly IPlanNcRepository _repository;
    private readonly IValidator<CreatePlanNcRequestDto> _createValidator;
    private readonly IPlanArchiverService _planArchiverService;
    private readonly ILogger<PlanNcService> _logger;

    public PlanNcService(
        IUnitOfWork unitOfWork,
        IPlanNcRepository repository,
        ILogger<PlanNcService> logger,
        IValidator<CreatePlanNcRequestDto> createValidator,
        IPlanArchiverService planArchiverService)
        : base(unitOfWork)
    {
        _repository = repository;
        _logger = logger;
        _createValidator = createValidator;
        _planArchiverService = planArchiverService;
    }

    // ==================== HOOKS IMPLÉMENTATION ====================

    /// <summary>
    /// Valide les règles métier spécifiques à la création d'un plan NC.
    /// </summary>
    protected override async Task<List<string>> ValidateCreationAsync(CreatePlanNcRequestDto dto)
    {
        var erreurs = new List<string>();

        ArgumentNullException.ThrowIfNull(_createValidator);
        var validationResult = await _createValidator.ValidateAsync(dto);

        if (validationResult is null)
        {
            erreurs.Add("Le validateur a retourné un résultat null.");
        }
        else if (!validationResult.IsValid)
        {
            erreurs.AddRange(validationResult.Errors.Select(e => e.ErrorMessage));
        }

        return erreurs;
    }

    /// <summary>
    /// Hook: Archiver l'ancien plan actif avant activation/versionning.
    /// </summary>
    protected override async Task HandleVersioningBeforeActivationAsync(PlanNcEntete plan, string user)
    {
        // Archiver l'ancien plan actif pour ce code poste
        var posteCode = plan.PosteCode;
        await _planArchiverService.ArchivePlanNcActifAsync(posteCode, user);
    }

    // ==================== ABSTRACT METHODS IMPLÉMENTATION ====================

    /// <summary>
    /// Récupère un plan NC par son ID.
    /// </summary>
    protected override async Task<PlanNcEntete?> ObtenirEntiteAsync(Guid id)
    {
        return await _repository.GetPlanAvecRelationsAsync(id);
    }

    /// <summary>
    /// Crée une nouvelle entité plan NC à partir du DTO de création.
    /// </summary>
    protected override async Task<PlanNcEntete> CreerEntiteAsync(CreatePlanNcRequestDto dto, string user)
    {
        var planId = Guid.NewGuid();
        var lines = new List<PlanNcLigne>();
        
        // Cache local pour éviter les doublons de défauts dans le même traitement
        var cacheDefauts = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        if (dto.Lignes != null && dto.Lignes.Any())
        {
            foreach (var l in dto.Lignes)
            {
                Guid resolvedId;
                var key = l.LibelleDefaut?.Trim();

                if (!string.IsNullOrWhiteSpace(key) && cacheDefauts.TryGetValue(key, out var cachedId))
                {
                    resolvedId = cachedId;
                }
                else
                {
                    resolvedId = await ResolveOrCreateRisqueDefautAsync(l);
                    if (!string.IsNullOrWhiteSpace(key) && resolvedId != Guid.Empty)
                    {
                        cacheDefauts[key] = resolvedId;
                    }
                }

                lines.Add(new PlanNcLigne
                {
                    Id = Guid.NewGuid(),
                    PlanNcenteteId = planId,
                    OrdreAffiche = l.OrdreAffiche,
                    MachineCode = l.MachineCode,
                    RisqueDefautId = resolvedId
                });
            }
        }

        return new PlanNcEntete
        {
            Id = planId,
            PosteCode = dto.PosteCode,
            Nom = dto.Nom,
            Version = 1,
            Statut = StatutsPlan.Actif,
            CreePar = user,
            CreeLe = DateTime.UtcNow,
            Remarques = dto.Remarques,
            LegendeMoyens = dto.LegendeMoyens,
            PlanNcLignes = lines
        };
    }

    protected override string GetStatutInitial() => StatutsPlan.Actif;

    /// <summary>
    /// Applique les mises à jour du DTO sur l'entité brouillon NC.
    /// </summary>
    protected override async Task ApplierMiseAJourDraftAsync(PlanNcEntete plan, SavePlanNcDto dto, string user)
    {
        plan.Nom = dto.Nom;
        plan.Remarques = dto.Remarques;
        plan.LegendeMoyens = dto.LegendeMoyens;
        plan.ModifiePar = user;
        plan.ModifieLe = DateTime.UtcNow;

        // Mettre à jour les lignes
        var dtoIds = dto.Lignes
            .Select(l => l.Id)
            .OfType<Guid>()
            .ToList();

        // Supprimer les lignes supprimées
        var lignesASupprimer = plan.PlanNcLignes.Where(l => !dtoIds.Contains(l.Id)).ToList();
        foreach (var ligne in lignesASupprimer)
        {
            _repository.RemoveLigne(ligne);
        }

        // Cache local pour éviter les doublons de défauts dans le même traitement
        var cacheDefauts = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        // Ajouter ou mettre à jour les lignes
        foreach (var ligneDto in dto.Lignes)
        {
            Guid resolvedId;
            var key = ligneDto.LibelleDefaut?.Trim();

            if (!string.IsNullOrWhiteSpace(key) && cacheDefauts.TryGetValue(key, out var cachedId))
            {
                resolvedId = cachedId;
            }
            else
            {
                resolvedId = await ResolveOrCreateRisqueDefautAsync(ligneDto);
                if (!string.IsNullOrWhiteSpace(key) && resolvedId != Guid.Empty)
                {
                    cacheDefauts[key] = resolvedId;
                }
            }
            
            var ligneEnBase = ligneDto.Id.HasValue && ligneDto.Id.Value != Guid.Empty
                ? plan.PlanNcLignes.FirstOrDefault(l => l.Id == ligneDto.Id.Value)
                : null;

            if (ligneEnBase != null)
            {
                PlanNcMapper.MettreAJourLigne(ligneEnBase, ligneDto, resolvedId);
            }
            else
            {
                var nouvelleLigne = PlanNcMapper.ConstruireNouvelleLigne(plan.Id, ligneDto, resolvedId);
                // Important: On l'ajoute à la collection du parent pour EF
                plan.PlanNcLignes.Add(nouvelleLigne);
                _repository.AddLigne(nouvelleLigne);
            }
        }
    }

    /// <summary>
    /// Persiste une nouvelle entité plan NC en base.
    /// </summary>
    protected override async Task PersisterEntiteAsync(PlanNcEntete plan)
    {
        await _repository.AddPlanAsync(plan);
    }

    /// <summary>
    /// Calcule la nouvelle version pour un plan NC.
    /// </summary>
    protected override async Task<int> CalculerNouvelleVersionAsync(PlanNcEntete plan)
    {
        var tousLesPlans = await _repository.GetTousLesPlansAsync();
        var maxVersion = tousLesPlans
            .Where(p => p.PosteCode == plan.PosteCode)
            .Max(p => (int?)p.Version) ?? 0;

        return maxVersion + 1;
    }

    /// <summary>
    /// Crée une nouvelle version du plan NC (copie).
    /// </summary>
    protected override async Task<PlanNcEntete> CreerNouvelleVersionEntiteAsync(
        PlanNcEntete ancienPlan, 
        SavePlanNcDto dto, 
        int nouvelleVersion, 
        string user)
    {
        var nouveauPlanId = Guid.NewGuid();
        var lines = new List<PlanNcLigne>();

        // Cache local pour éviter les doublons de défauts dans le même traitement
        var cacheDefauts = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        foreach (var l in dto.Lignes)
        {
            Guid resolvedId;
            var key = l.LibelleDefaut?.Trim();

            if (!string.IsNullOrWhiteSpace(key) && cacheDefauts.TryGetValue(key, out var cachedId))
            {
                resolvedId = cachedId;
            }
            else
            {
                resolvedId = await ResolveOrCreateRisqueDefautAsync(l);
                if (!string.IsNullOrWhiteSpace(key) && resolvedId != Guid.Empty)
                {
                    cacheDefauts[key] = resolvedId;
                }
            }

            lines.Add(new PlanNcLigne
            {
                Id = Guid.NewGuid(),
                PlanNcenteteId = nouveauPlanId,
                OrdreAffiche = l.OrdreAffiche,
                MachineCode = l.MachineCode,
                RisqueDefautId = resolvedId
            });
        }

        return new PlanNcEntete
        {
            Id = nouveauPlanId,
            PosteCode = ancienPlan.PosteCode,
            Nom = dto.Nom,
            Version = nouvelleVersion,
            Statut = StatutsPlan.Actif,
            CreePar = user,
            CreeLe = DateTime.UtcNow,
            Remarques = dto.Remarques,
            LegendeMoyens = dto.LegendeMoyens,
            PlanNcLignes = lines
        };
    }

    /// <summary>
    /// Vérifie si un brouillon identique existe déjà (par code poste).
    /// </summary>
    protected override async Task<PlanNcEntete?> ObtenirBrouillonExistantAsync(CreatePlanNcRequestDto dto)
    {
        var tousLesPlans = await _repository.GetTousLesPlansAsync();
        // Pour les NC, on considère qu'il n'y a qu'un seul plan "de référence" par poste.
        // Si un plan (Actif ou Brouillon) existe déjà pour ce poste, on le retourne pour éviter les doublons.
        return tousLesPlans.FirstOrDefault(p => 
            p.PosteCode == dto.PosteCode && 
            (p.Statut == StatutsPlan.Brouillon || p.Statut == StatutsPlan.Actif));
    }

    // ==================== PUBLIC METHODS ====================

    /// <summary>
    /// Récupère un plan NC par son ID.
    /// </summary>
    public async Task<PlanNcResponseDto> GetPlanByIdAsync(Guid planId)
    {
        var plan = await _repository.GetPlanAvecRelationsAsync(planId);
        if (plan == null) throw new InvalidOperationException("Plan introuvable.");
        return PlanNcMapper.MapperEntiteVersDto(plan);
    }

    /// <summary>
    /// Récupère tous les plans NC.
    /// </summary>
    public async Task<List<PlanNcResponseDto>> GetTousLesPlansAsync()
    {
        var plans = await _repository.GetTousLesPlansAsync();
        return plans.Select(p => PlanNcMapper.MapperEntiteVersDto(p)).ToList();
    }

    /// <summary>
    /// Crée un nouveau plan NC en BROUILLON.
    /// Public wrapper pour IPlanNcService.
    /// </summary>
    public async Task<Guid> CreerPlanAsync(CreatePlanNcRequestDto request, string creePar)
    {
        return await CreerBrouillonAsync(request, creePar);
    }

    /// <summary>
    /// Met à jour un plan NC en BROUILLON avec les nouvelles lignes.
    /// </summary>
    public async Task<Guid> MettreAJourPlanAsync(Guid planId, SavePlanNcDto request, string modifiePar)
    {
        var plan = await _repository.GetPlanAvecRelationsAsync(planId);
        if (plan == null) throw new KeyNotFoundException("Plan introuvable.");

        // On bypass UpdateDraftAsync car il refuse de modifier les plans ACTIFS.
        // Pour les résultats de contrôle, on autorise la modification directe.
        await ApplierMiseAJourDraftAsync(plan, request, SecuriserNomAuteur(modifiePar));

        if (plan.Statut == StatutsPlan.Brouillon)
        {
            plan.Statut = StatutsPlan.Actif;
        }

        await _unitOfWork.CommitAsync();
        return plan.Id;
    }

    /// <summary>
    /// Met à jour les lignes d'un plan NC en BROUILLON.
    /// </summary>
    public async Task<bool> MettreAJourLignesAsync(Guid planId, List<LigneNcEditDto> lignesModifiees)
    {
        var plan = await _repository.GetPlanAvecRelationsAsync(planId);
        if (plan == null) return false;

        var dtoIds = lignesModifiees
            .Select(l => l.Id)
            .OfType<Guid>()
            .ToList();

        // Supprimer les lignes supprimées
        var lignesASupprimer = plan.PlanNcLignes.Where(l => !dtoIds.Contains(l.Id)).ToList();
        foreach (var ligne in lignesASupprimer)
        {
            _repository.RemoveLigne(ligne);
        }

        // Ajouter ou mettre à jour les lignes
        foreach (var ligneDto in lignesModifiees)
        {
            var resolvedId = await ResolveOrCreateRisqueDefautAsync(ligneDto);
            
            var isNew = !ligneDto.Id.HasValue || ligneDto.Id.Value == Guid.Empty;
            var ligneEnBase = !isNew && ligneDto.Id is Guid ligneId
                ? plan.PlanNcLignes.FirstOrDefault(l => l.Id == ligneId)
                : null;

            if (ligneEnBase != null)
            {
                PlanNcMapper.MettreAJourLigne(ligneEnBase, ligneDto, resolvedId);
            }
            else
            {
                var nouvelleLigne = PlanNcMapper.ConstruireNouvelleLigne(planId, ligneDto, resolvedId);
                _repository.AddLigne(nouvelleLigne);
            }
        }

        // Auto-activation si le plan était en BROUILLON
        if (plan.Statut == StatutsPlan.Brouillon)
        {
            plan.Statut = StatutsPlan.Actif;
        }

        await _repository.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Crée une nouvelle version d'un plan NC existant.
    /// </summary>
    public async Task<Guid> CreerNouvelleVersionAsync(NouvelleVersionNcRequestDto request)
    {
        var ancienPlan = await _repository.GetPlanAvecRelationsAsync(request.AncienId);
        if (ancienPlan == null) throw new InvalidOperationException("Plan introuvable.");

        // Créer une copie du plan en brouillon
        var nouveauPlan = new PlanNcEntete
        {
            Id = Guid.NewGuid(),
            PosteCode = ancienPlan.PosteCode,
            Nom = ancienPlan.Nom,
            Version = ancienPlan.Version + 1,
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

    /// <summary>
    /// Restaure un plan NC archivé.
    /// </summary>
    public async Task<Guid> RestaurerPlanAsync(Guid planId, string restaurePar, string motif)
    {
        var planArchive = await _repository.GetPlanAvecRelationsAsync(planId);
        if (planArchive == null) throw new InvalidOperationException("Plan archivé introuvable.");

        if (planArchive.Statut != StatutsPlan.Archive)
            throw new InvalidOperationException("Seul un plan archivé peut être restauré.");

        // Archiver le plan actif actuel
        var planActuel = await _repository.GetPlanActifAsync(planArchive.PosteCode);
        if (planActuel != null && planActuel.Id != planArchive.Id)
        {
            planActuel.Statut = StatutsPlan.Archive;
            planActuel.ModifiePar = restaurePar;
            planActuel.ModifieLe = DateTime.UtcNow;
        }

        // Calculer la nouvelle version
        var tousLesPlans = await _repository.GetTousLesPlansAsync();
        var maxVersion = tousLesPlans
            .Where(p => p.PosteCode == planArchive.PosteCode)
            .Max(p => (int?)p.Version) ?? 0;

        // Créer une nouvelle version basée sur l'archive
        var nouveauPlanId = Guid.NewGuid();
        var nouveauPlan = new PlanNcEntete
        {
            Id = nouveauPlanId,
            PosteCode = planArchive.PosteCode,
            Nom = planArchive.Nom,
            Version = maxVersion + 1,
            Statut = StatutsPlan.Actif,
            CreePar = restaurePar,
            CreeLe = DateTime.UtcNow,
            PlanNcLignes = planArchive.PlanNcLignes.Select(l => new PlanNcLigne
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
    /// <summary>
    /// Résout un RisqueDefautId à partir d'un DTO, en créant le défaut s'il n'existe pas.
    /// </summary>
    private async Task<Guid> ResolveOrCreateRisqueDefautAsync(LigneNcEditDto dto)
    {
        if (dto.RisqueDefautId.HasValue && dto.RisqueDefautId.Value != Guid.Empty)
            return dto.RisqueDefautId.Value;

        if (string.IsNullOrWhiteSpace(dto.LibelleDefaut))
            return Guid.Empty;

        var libelle = dto.LibelleDefaut.Trim();

        // 1. Chercher par libellé (Simple et direct)
        var existant = await _unitOfWork.DictionnaireQualiteRepository.GetRisqueDefautByLibelleAsync(libelle);
        if (existant != null) return existant.Id;

        // 2. Créer un nouveau défaut (Le code devient automatique et identique au libellé)
        var nouveau = new RisqueDefaut
        {
            Id = Guid.NewGuid(),
            // On prend le libellé comme code (tronqué à 30 car c'est la limite de la colonne CodeDefaut)
            CodeDefaut = libelle.Length > 30 ? libelle.Substring(0, 30).ToUpper() : libelle.ToUpper(),
            LibelleDefaut = libelle,
            Actif = true
        };

        await _unitOfWork.DictionnaireQualiteRepository.AddRisqueDefautAsync(nouveau);
        return nouveau.Id;
    }
}
