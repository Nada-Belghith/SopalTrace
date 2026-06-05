using FluentValidation;
using Microsoft.Extensions.Logging;
using SopalTrace.Application.DTOs.QualityPlans.PlanAssemblage;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Mappers;
using SopalTrace.Application.Specifications;
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
/// Service de gestion des plans d'assemblage
/// Gère la création, modification et versioning des plans
/// </summary>
public class PlanAssService : IPlanAssService
{
    private readonly IPlanAssRepository _repository;
    private readonly IDictionnaireQualiteRepository _dicoRepository;
    private readonly IValidator<CreatePlanAssDto> _createValidator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<PlanAssService> _logger;
    private readonly IPlanArchiverService _planArchiverService;

    public PlanAssService(
        IPlanAssRepository repository,
        IDictionnaireQualiteRepository dicoRepository,
        IValidator<CreatePlanAssDto> createValidator,
        ICurrentUserService currentUserService,
        ILogger<PlanAssService> logger,
        IPlanArchiverService planArchiverService)
    {
        _repository = repository;
        _dicoRepository = dicoRepository;
        _createValidator = createValidator;
        _currentUserService = currentUserService;
        _logger = logger;
        _planArchiverService = planArchiverService;
    }

    private async Task<int> CalculerNouvelleVersionAsync(string operationCode, string familleCode, string? codeArticle = null)
    {
        return (await _repository.GetDerniereVersionAsync(operationCode, familleCode, codeArticle)) + 1;
    }

    public async Task<Guid> CreerPlanAssemblageAsync(CreatePlanAssDto request)
    {
        _logger.LogInformation(
            "Creation plan assemblage. Type: {TypePlan}, Operation: {OperationCode}, Nature: {NatureComposantCode}, Poste: {PosteCode}",
            request.EstModele ? "MODELE" : "EXCEPTION",
            request.OperationCode,
            request.NatureComposantCode,
            request.PosteCode);

        await ValidationHelper.ValidateAndThrowAsync(_createValidator, request, "CreatePlanAssemblageRequest");

        var estModele = request.EstModele;

        // Validation de la Gamme Opératoire
        if (!await _repository.IsOperationValidePourNatureAsync(request.NatureComposantCode, request.OperationCode))
        {
            throw new ValidationException($"L'opération '{request.OperationCode}' n'est pas autorisée pour un article de nature '{request.NatureComposantCode}'.");
        }
        var codeArticle = estModele ? null : request.CodeArticleSage;
        string? designationSage = null;

        if (estModele)
        {
            var planMaitreExists = await _repository.ExistePlanMaitreActifAsync(
                request.OperationCode,
                MapperHelper.NullIfEmpty(request.FamilleCode),
                request.NatureComposantCode,
                request.PosteCode);

            PlanAssSpecification.ValidatePlanMaitreCreation(
                planMaitreExists,
                request.OperationCode,
                request.TypeRobinetCode ?? request.NatureComposantCode);
        }
        else
        {
            PlanAssSpecification.ValidateArticleCodeForException(codeArticle);

            var planExceptionExists = await _repository.ExisteExceptionActiveAsync(
                request.OperationCode,
                MapperHelper.NullIfEmpty(request.FamilleCode),
                request.NatureComposantCode,
                request.PosteCode,
                codeArticle!);

            PlanAssSpecification.ValidatePlanExceptionCreation(
                planExceptionExists,
                request.OperationCode,
                request.TypeRobinetCode ?? request.NatureComposantCode,
                codeArticle!);

            designationSage = await _repository.GetDesignationArticleSageAsync(codeArticle!);
            PlanAssSpecification.ValidateArticleExistsInErp(designationSage, codeArticle!);
        }

        var nextVersion = request.VersionInitiale ?? await CalculerNouvelleVersionAsync(
            request.OperationCode,
            MapperHelper.NullIfEmpty(request.FamilleCode),
            codeArticle);

        var planId = Guid.NewGuid();
        var nouveauPlan = new PlanAssemblageEntete
        {
            Id = planId,
            OperationCode = request.OperationCode,
            FamilleProduitFiniCode = MapperHelper.NullIfEmpty(request.FamilleCode),
            Designation = designationSage ?? request.Nom, // On utilise le Nom du DTO comme designation par defaut si non article
            Version = nextVersion,
            Statut = StatutsPlan.Brouillon,
            CreePar = _currentUserService.UserInfo,
            CreeLe = DateTime.UtcNow,
            LegendeMoyens = request.LegendeMoyens,
            //Remarques = request.Remarques,
            PlanAssemblageSections = new List<PlanAssemblageSection>()
        };

        foreach (var sectionDto in request.Sections ?? new List<SectionAssEditDto>())
        {
            var section = PlanAssMapper.ConstruireNouvelleSection(planId, sectionDto);
            section.Id = sectionDto.Id.GetValueOrDefault(Guid.NewGuid());

            foreach (var ligneDto in sectionDto.Lignes ?? new List<LigneAssEditDto>())
            {
                var ligne = PlanAssMapper.ConstruireNouvelleLigne(planId, section.Id, ligneDto);
                ligne.Id = ligneDto.Id.GetValueOrDefault(Guid.NewGuid());
                section.PlanAssemblageLignes.Add(ligne);
            }

            // Résolution intelligente de la règle d'échantillonnage
            if (section.RegleEchantillonnageId == null && !string.IsNullOrWhiteSpace(section.RegleEchantillonnageLibelle))
            {
                section.RegleEchantillonnageId = await SamplingRuleHelper.ResolveOrCreateSamplingRuleByLibelleAsync(
                    _dicoRepository,
                    section.RegleEchantillonnageLibelle);
            }

            nouveauPlan.PlanAssemblageSections.Add(section);
        }

        // Final cleanup to enforce XOR constraint on means of control
        foreach (var sec in nouveauPlan.PlanAssemblageSections)
        {
            foreach (var l in sec.PlanAssemblageLignes)
            {
                LineCleanupHelper.CleanupPlanAssLine(l);
            }
        }

        await _repository.AddPlanAsync(nouveauPlan);
        await _repository.SaveChangesAsync();

        _logger.LogInformation(
            "Plan assemblage cree dans Plan_Ass_Entete. ID: {PlanId}, Version: {Version}",
            nouveauPlan.Id,
            nouveauPlan.Version);

        return nouveauPlan.Id;
    }

    /// <summary>
    /// Crée un nouveau plan d'assemblage (maître ou exception)
    /// </summary>
    public async Task<Guid> CreerPlanAsync(CreatePlanAssRequestDto request, string creePar)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        return await CreerPlanAssemblageAsync(request with { CreePar = creePar });
    }

    /// <summary>
    /// Récupère un plan avec toutes ses relations
    /// </summary>
    public async Task<PlanAssResponseDto> GetPlanByIdAsync(Guid planId)
    {
        _logger.LogInformation("Récupération du plan avec l'ID: {PlanId}", planId);

        try
        {
            var plan = await _repository.GetPlanAvecRelationsAsync(planId);
            PlanAssSpecification.ValidatePlanExists(plan, planId);

            var dto = PlanAssMapper.MapperEntitePlanVersDto(plan!);
            _logger.LogInformation("Plan récupéré avec succès. ID: {PlanId}, Statut: {Statut}",
                planId, plan!.Statut);

            return dto;
        }
        catch (PlanNotFoundException ex)
        {
            _logger.LogWarning("Plan introuvable: {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération du plan {PlanId}", planId);
            throw;
        }
    }

    /// <summary>
    /// Met à jour les valeurs d'un plan (sections et lignes)
    /// </summary>
    public async Task<bool> MettreAJourValeursPlanAsync(Guid planId, List<SectionAssEditDto> sectionsModifiees, bool finaliser)
    {
        _logger.LogInformation("Début de la mise à jour des valeurs du plan {PlanId}", planId);

        try
        {
            var plan = await _repository.GetPlanAvecRelationsAsync(planId);
            if (plan is null)
            {
                _logger.LogWarning("Plan non trouvé pour la mise à jour: {PlanId}", planId);
                return false;
            }

            SectionUpdateHelper.UpdateSections(
                plan.PlanAssemblageSections,
                sectionsModifiees,
                deleteSection: section => { /* Deletion is handled by collection removal */ },
                deleteLine: line => { /* Deletion is handled by collection removal */ },
                getSectionDtoId: dto => dto.Id,
                getLineDtoId: dto => dto.Id,
                getSectionId: section => section.Id,
                getLineId: line => line.Id,
                createSection: dto => PlanAssMapper.ConstruireNouvelleSection(planId, dto),
                updateSection: (section, dto) =>
                {
                    section.TypeSectionId = dto.TypeSectionId;
                    section.PeriodiciteId = dto.PeriodiciteId ?? Guid.Empty;
                    section.OrdreAffiche = dto.OrdreAffiche;
                    section.LibelleSection = dto.LibelleSection ?? section.LibelleSection;
                    section.NormeReference = dto.NormeReference;
                    section.NqaId = dto.NqaId;
                    section.Notes = dto.Notes;
                    section.RegleEchantillonnageId = dto.RegleEchantillonnageId;
                    section.RegleEchantillonnageLibelle = dto.RegleEchantillonnageLibelle;
                },
                getLines: section => section.PlanAssemblageLignes,
                getLineDtos: dto => dto.Lignes,
                createLine: (dto, section) => PlanAssMapper.ConstruireNouvelleLigne(planId, section.Id, dto),
                updateLine: (line, dto) => PlanAssMapper.MettreAJourEntiteLigne(line, dto)
            );

            // Post-processing specific to PlanAssService
            foreach (var section in plan.PlanAssemblageSections)
            {
                if (section.RegleEchantillonnageId == null && !string.IsNullOrWhiteSpace(section.RegleEchantillonnageLibelle))
                {
                    section.RegleEchantillonnageId = await SamplingRuleHelper.ResolveOrCreateSamplingRuleByLibelleAsync(
                        _dicoRepository,
                        section.RegleEchantillonnageLibelle);
                }

                foreach (var ligne in section.PlanAssemblageLignes)
                {
                    if (!string.IsNullOrWhiteSpace(ligne.LibelleAffiche) && ligne.TypeCaracteristiqueId == Guid.Empty)
                    {
                        ligne.TypeCaracteristiqueId = (await TypeCaracteristiqueHelper.ResolveOrCreateTypeCaracteristiqueByLibelleAsync(
                            _dicoRepository,
                            ligne.LibelleAffiche)) ?? Guid.Empty;
                    }

                    LineCleanupHelper.CleanupPlanAssLine(ligne);
                }
            }

            // Activation + archivage de l'ancienne version
            if (finaliser && plan.Statut == StatutsPlan.Brouillon)
            {
                plan.Statut = StatutsPlan.Actif;

                // Utiliser FormulaireId en priorité pour distinguer les plans indépendants
                PlanAssemblageEntete? ancienPlanActif = null;
                if (plan.FormulaireId.HasValue)
                {
                    ancienPlanActif = await _repository.GetPlanActifParFormulaireAsync(plan.FormulaireId.Value);
                }
                if (ancienPlanActif == null)
                {
                    // Fallback : recherche par critères combinés
                    ancienPlanActif = await _repository.GetPlanActifMaitreAsync(plan.OperationCode, plan.FamilleProduitFiniCode, plan.NatureArticleCode, plan.PosteCode);
                }

                if (ancienPlanActif is not null && ancienPlanActif.Id != plan.Id)
                {
                    ancienPlanActif.Statut = StatutsPlan.Archive;
                    ancienPlanActif.ModifiePar = PlanMetadataHelper.NormalizeAuthorNameWithTruncation(plan.ModifiePar);
                    ancienPlanActif.ModifieLe = DateTime.UtcNow;
                }
            }

            await _repository.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la mise à jour du plan {PlanId}", planId);
            throw;
        }
    }

    public async Task<bool> ChangerStatutPlanAsync(Guid planId, ChangePlanAssStatusRequestDto request, string modifiePar)
    {
        try
        {
            var plan = await _repository.GetPlanAvecRelationsAsync(planId);
            PlanAssSpecification.ValidatePlanExists(plan, planId);

            plan!.Statut = request.NouveauStatut;
            plan.ModifiePar = PlanMetadataHelper.NormalizeAuthorNameWithTruncation(modifiePar);
            plan.ModifieLe = DateTime.UtcNow;

            await _repository.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du changement de statut du plan {PlanId}", planId);
            throw;
        }
    }

    public async Task<Guid> ClonerExceptionDepuisMaitreAsync(CloneExceptionAssRequestDto request)
    {
        try
        {
            var planMaitre = await _repository.GetPlanAvecRelationsAsync(request.PlanMaitreId);
            PlanAssSpecification.ValidatePlanExists(planMaitre, request.PlanMaitreId);

            var designation = await _repository.GetDesignationArticleSageAsync(request.NouveauCodeArticleSage);
            PlanAssSpecification.ValidateArticleExistsInErp(designation, request.NouveauCodeArticleSage);

            var planDuplique = PlanAssMapper.DupliquerEntitePlan(
                planMaitre!,
                estModele: false,
                nouveauCodeArticle: request.NouveauCodeArticleSage,
                nouvelleDesig: designation,
                creePar: request.CreePar,
                motif: "Cloné à partir du plan maître");

            await _repository.AddPlanAsync(planDuplique);
            await _repository.SaveChangesAsync();

            return planDuplique.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du clonage de l'exception");
            throw;
        }
    }

    public async Task<Guid> CreerNouvelleVersionPlanAsync(NouvelleVersionAssRequestDto request)
    {
        try
        {
            var planSource = await _repository.GetPlanAvecRelationsAsync(request.AncienId);
            PlanAssSpecification.ValidatePlanExists(planSource, request.AncienId);

            // ✅ Normaliser le nom si c'est un nom technique hérité ("Modèle PLAN-ASS-...", "PC-...")
            var designation = planSource!.Designation;
            if (!string.IsNullOrWhiteSpace(designation) &&
                (designation.StartsWith("Modèle ") || System.Text.RegularExpressions.Regex.IsMatch(designation, @"^PC-[A-Z0-9]")))
            {
                designation = planSource.NatureArticleCode switch
                {
                    "PISTON" => "Plan de contrôle en cours de fabrication piston",
                    "PF"     => "Plan en cours de fabrication produit fini",
                    _        => designation
                };
            }

            var planDuplique = PlanAssMapper.DupliquerEntitePlan(
                planSource!,
                estModele: true,
                nouveauCodeArticle: null,
                nouvelleDesig: designation,
                creePar: request.CreePar,
                motif: request.MotifModification);

            // ✅ Forcer la préservation de NatureArticleCode depuis la source (évite les NULL en DB)
            planDuplique.NatureArticleCode = planSource.NatureArticleCode;
            planDuplique.Version = request.VersionInitiale ?? planDuplique.Version;

            await _repository.AddPlanAsync(planDuplique);
            await _repository.SaveChangesAsync();

            return planDuplique.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création d'une nouvelle version du plan");
            throw;
        }
    }
}

