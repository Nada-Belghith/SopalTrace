using FluentValidation;
using Microsoft.Extensions.Logging;
using SopalTrace.Application.DTOs.QualityPlans.Modeles;
using SopalTrace.Application.DTOs.QualityPlans.Plans;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Mappers;
using SopalTrace.Domain.Exceptions;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities; // <-- LA CORRECTION DE L'ESPACE DE NOMS EST ICI !
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Application.Services;

public class PlanFabricationService : IPlanFabricationService
{
    private readonly IPlanFabricationRepository _repository;
    private readonly ILogger<PlanFabricationService> _logger;
    private readonly IValidator<CreateModeleRequestDto> _modeleValidator;
    private readonly IValidator<CreatePlanRequestDto> _createPlanValidator;
    private readonly IValidator<ClonePlanRequestDto> _clonePlanValidator;

    public PlanFabricationService(
        IPlanFabricationRepository repository,
        ILogger<PlanFabricationService> logger,
        IValidator<CreateModeleRequestDto> modeleValidator,
        IValidator<CreatePlanRequestDto> createPlanValidator,
        IValidator<ClonePlanRequestDto> clonePlanValidator)
    {
        _repository = repository;
        _logger = logger;
        _modeleValidator = modeleValidator;
        _createPlanValidator = createPlanValidator;
        _clonePlanValidator = clonePlanValidator;
    }

    // =========================================================================================
    // 1. GESTION DES MODÈLES
    // =========================================================================================

    public async Task<Guid> CreerModeleAsync(CreateModeleRequestDto request)
    {
        _logger.LogInformation("Début de création d'un modèle (Code: {Code})", request.Code);

        var validationResult = await _modeleValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        if (await _repository.ExisteModeleActifAsync(request.TypeRobinetCode, request.NatureComposantCode, request.OperationCode))
            throw new DoublonModeleException();

        var nouveauModele = PlanFabricationMapper.ConstruireEntiteModeleAPartirDeDto(request);

        await _repository.AddModeleAsync(nouveauModele);
        await _repository.SaveChangesAsync();

        return nouveauModele.Id;
    }

    public async Task<ModeleResponseDto> GetModeleByIdAsync(Guid modeleId)
    {
        var modele = await _repository.GetModeleAvecRelationsAsync(modeleId);
        if (modele == null) throw new ModeleIntrouvableException(modeleId);

        return PlanFabricationMapper.MapperEntiteModeleVersDto(modele);
    }

    // =========================================================================================
    // 2. GESTION DES PLANS
    // =========================================================================================

    public async Task<Guid> InstancierPlanDepuisModeleAsync(CreatePlanRequestDto request)
    {
        var validationResult = await _createPlanValidator.ValidateAsync(request);
        if (!validationResult.IsValid) throw new ValidationException(validationResult.Errors);

        var designationSage = await _repository.GetDesignationArticleSageAsync(request.CodeArticleSage);
        if (string.IsNullOrEmpty(designationSage))
            throw new ArticleSageIntrouvableException(request.CodeArticleSage);

        if (await _repository.ExistePlanActifPourArticleAsync(request.CodeArticleSage))
            throw new DoublonPlanException(request.CodeArticleSage);

        var modeleSource = await _repository.GetModeleActifAvecRelationsAsync(request.ModeleSourceId);
        if (modeleSource == null) throw new ModeleIntrouvableException(request.ModeleSourceId);

        var nouveauPlan = PlanFabricationMapper.ConstruireEntitePlanAPartirDeModele(modeleSource, request, designationSage);

        await _repository.AddPlanAsync(nouveauPlan);
        await _repository.SaveChangesAsync();

        return nouveauPlan.Id;
    }

    public async Task<PlanResponseDto> GetPlanByIdAsync(Guid planId)
    {
        var plan = await _repository.GetPlanAvecRelationsAsync(planId);
        if (plan == null) throw new PlanIntrouvableException(planId);

        return PlanFabricationMapper.MapperEntitePlanVersDto(plan);
    }

    public async Task<bool> MettreAJourValeursPlanAsync(Guid planId, List<SectionEditDto> sectionsModifiees)
    {
        var plan = await _repository.GetPlanCompletPourMiseAJourAsync(planId);
        if (plan == null) return false;

        foreach (var secDto in sectionsModifiees)
        {
            // CORRECTION: Gestion sécurisée des IDs potentiellement nuls pour éviter les crashs
            var section = secDto.Id.HasValue ? plan.PlanFabSections.FirstOrDefault(s => s.Id == secDto.Id.Value) : null;

            if (section != null)
            {
                section.OrdreAffiche = secDto.OrdreAffiche;
                section.LibelleSection = secDto.LibelleSection ?? section.LibelleSection;
            }
            else
            {
                section = PlanFabricationMapper.ConstruireNouvelleSectionPlan(planId, secDto);
                plan.PlanFabSections.Add(section);
            }

            foreach (var ligDto in secDto.Lignes)
            {
                var ligne = ligDto.Id.HasValue ? section.PlanFabLignes.FirstOrDefault(l => l.Id == ligDto.Id.Value) : null;

                if (ligne != null)
                {
                    PlanFabricationMapper.MettreAJourEntiteLigne(ligne, ligDto);
                }
                else
                {
                    var nouvelleLigne = PlanFabricationMapper.ConstruireNouvelleLignePlan(planId, section.Id, ligDto);
                    section.PlanFabLignes.Add(nouvelleLigne);
                }
            }
        }

        if (plan.Statut == StatutsPlan.Brouillon)
        {
            // CORRECTION: L'appel ne prend qu'un seul argument (CodeArticleSage)
            var ancienPlanActif = await _repository.GetPlanActifPourArticleAsync(plan.CodeArticleSage);
            if (ancienPlanActif != null && ancienPlanActif.Id != plan.Id)
            {
                ancienPlanActif.Statut = StatutsPlan.Archive;
                ancienPlanActif.ModifiePar = plan.ModifiePar ?? "SYSTEM";
                ancienPlanActif.ModifieLe = DateTime.UtcNow;
            }

            plan.Statut = StatutsPlan.Actif;
            plan.DateApplication = DateOnly.FromDateTime(DateTime.UtcNow);
        }

        await _repository.SaveChangesAsync();
        return true;
    }

    // =========================================================================================
    // 3. CLONAGE ET VERSIONING
    // =========================================================================================

    public async Task<bool> ChangerStatutPlanAsync(Guid planId, ChangePlanStatusRequestDto request, string modifiePar)
    {
        var plan = await _repository.GetPlanByIdAsync(planId);
        if (plan == null)
            throw new PlanIntrouvableException(planId);

        if (plan.Statut == request.NouveauStatut)
            return false;

        // Sécurité pour éviter le dépassement de la taille en base de données
        var modifieParSecure = modifiePar?.Length > 20 ? modifiePar.Substring(0, 20) : modifiePar;

        plan.Statut = request.NouveauStatut;
        plan.ModifiePar = modifieParSecure;
        plan.ModifieLe = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(request.Motif))
            plan.CommentaireVersion = request.Motif;

        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<Guid> ClonerPlanPourNouvelArticleAsync(ClonePlanRequestDto request)
    {
        var validationResult = await _clonePlanValidator.ValidateAsync(request);
        if (!validationResult.IsValid) throw new ValidationException(validationResult.Errors);

        var designationSage = await _repository.GetDesignationArticleSageAsync(request.NouveauCodeArticleSage);
        if (string.IsNullOrEmpty(designationSage))
            throw new ArticleSageIntrouvableException(request.NouveauCodeArticleSage);

        if (await _repository.ExistePlanActifPourArticleAsync(request.NouveauCodeArticleSage))
            throw new DoublonPlanException(request.NouveauCodeArticleSage);

        var planSource = await _repository.GetPlanAvecRelationsAsync(request.PlanExistantId);
        if (planSource == null) throw new PlanIntrouvableException(request.PlanExistantId);
        if (planSource.Statut != StatutsPlan.Actif) throw new PlanSourceNonActifException(request.PlanExistantId);

        var creeParSecure = request.CreePar?.Length > 20 ? request.CreePar.Substring(0, 20) : request.CreePar;

        var planClone = PlanFabricationMapper.DupliquerEntitePlan(planSource, request.NouveauCodeArticleSage, designationSage, creeParSecure);

        await _repository.AddPlanAsync(planClone);
        await _repository.SaveChangesAsync();

        return planClone.Id;
    }

    public async Task<Guid> CreerNouvelleVersionPlanAsync(NouvelleVersionRequestDto request)
    {
        var ancienPlan = await _repository.GetPlanAvecRelationsAsync(request.AncienId);
        if (ancienPlan == null) throw new PlanIntrouvableException(request.AncienId);
        if (ancienPlan.Statut == StatutsPlan.Archive) throw new PlanArchiveException();
        if (ancienPlan.Statut != StatutsPlan.Actif) throw new PlanSourceNonActifException(request.AncienId);

        var modifieParSecure = request.ModifiePar?.Length > 20 ? request.ModifiePar.Substring(0, 20) : request.ModifiePar;

        // V1 reste ACTIF ici. L'archivage se fera au moment de l'activation de la V2 dans MettreAJourValeursPlanAsync.
        var nouveauPlan = PlanFabricationMapper.DupliquerEntitePlan(
            ancienPlan,
            ancienPlan.CodeArticleSage,
            ancienPlan.Designation ?? $"Copy-{ancienPlan.Id}",
            modifieParSecure,
            request.MotifModification);

        await _repository.AddPlanAsync(nouveauPlan);
        await _repository.SaveChangesAsync();

        return nouveauPlan.Id;
    }
}