using FluentValidation;
using Microsoft.Extensions.Logging;
using SopalTrace.Application.DTOs.QualityPlans.PlanAssemblage;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Mappers;
using SopalTrace.Application.Specifications;
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
    private readonly IValidator<CreatePlanAssRequestDto> _createValidator;
    private readonly ILogger<PlanAssService> _logger;

    public PlanAssService(
        IPlanAssRepository repository,
        IValidator<CreatePlanAssRequestDto> createValidator,
        ILogger<PlanAssService> logger)
    {
        _repository = repository;
        _createValidator = createValidator;
        _logger = logger;
    }

    /// <summary>
    /// Crée un nouveau plan d'assemblage (maître ou exception)
    /// </summary>
    public async Task<Guid> CreerPlanAsync(CreatePlanAssRequestDto request, string creePar)
    {
        _logger.LogInformation(
            "Début de création d'un plan d'assemblage. Type: {TypePlan}, OpCode: {OperationCode}, TRobinet: {TypeRobinet}",
            request.EstModele ? "MAÎTRE" : "EXCEPTION",
            request.OperationCode,
            request.TypeRobinetCode);

        try
        {
            var validationResult = await _createValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Validation échouée pour la création du plan. Erreurs: {Erreurs}",
                    string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));
                throw new ValidationException(validationResult.Errors);
            }

            var estModele = request.EstModele;
            var codeArticle = estModele ? null : request.CodeArticleSage;
            string? designationSage = null;

            if (estModele)
            {
                var planMaitreExists = await _repository.ExistePlanMaitreActifAsync(
                    request.OperationCode, request.TypeRobinetCode);
                PlanAssSpecification.ValidatePlanMaitreCreation(
                    planMaitreExists, request.OperationCode, request.TypeRobinetCode);

                _logger.LogInformation("Création d'un Plan Maître pour l'opération {OperationCode}",
                    request.OperationCode);
            }
            else
            {
                PlanAssSpecification.ValidateArticleCodeForException(codeArticle);

                var planExceptionExists = await _repository.ExisteExceptionActiveAsync(
                    request.OperationCode, request.TypeRobinetCode, codeArticle!);
                PlanAssSpecification.ValidatePlanExceptionCreation(
                    planExceptionExists, request.OperationCode, request.TypeRobinetCode, codeArticle!);

                designationSage = await _repository.GetDesignationArticleSageAsync(codeArticle!);
                PlanAssSpecification.ValidateArticleExistsInErp(designationSage, codeArticle!);

                _logger.LogInformation(
                    "Création d'une Exception de Plan pour l'article {CodeArticle} (Désignation: {Designation})",
                    codeArticle, designationSage);
            }

            var nextVersion = await _repository.GetDerniereVersionAsync(
                request.OperationCode, request.TypeRobinetCode, codeArticle) + 1;

            var nouveauPlan = new PlanAssEntete
            {
                Id = Guid.NewGuid(),
                OperationCode = request.OperationCode,
                TypeRobinetCode = request.TypeRobinetCode,
                EstModele = estModele,
                CodeArticleSage = codeArticle,
                Nom = request.Nom,
                Designation = designationSage,
                Version = nextVersion,
                Statut = StatutsPlan.Brouillon,
                CreePar = Truncate20(creePar),
                CreeLe = DateTime.UtcNow,
                CommentaireVersion = request.CommentaireVersion ?? "Création initiale",
                PlanAssSections = new List<PlanAssSection>()
            };

            await _repository.AddPlanAsync(nouveauPlan);
            await _repository.SaveChangesAsync();

            _logger.LogInformation(
                "Plan d'assemblage créé avec succès. ID: {PlanId}, Version: {Version}, Créé par: {CreePar}",
                nouveauPlan.Id, nouveauPlan.Version, creePar);

            return nouveauPlan.Id;
        }
        catch (DomainException ex)
        {
            _logger.LogWarning("Erreur métier lors de la création du plan: {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur non prévue lors de la création du plan");
            throw;
        }
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
    public async Task<bool> MettreAJourValeursPlanAsync(Guid planId, List<SectionAssEditDto> sectionsModifiees)
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

            // 1. SUPPRESSION des sections retirées par le front
            var dtoSectionIds = sectionsModifiees.Where(s => s.Id.HasValue).Select(s => s.Id.Value).ToList();
            var sectionsToRemove = plan.PlanAssSections.Where(s => !dtoSectionIds.Contains(s.Id)).ToList();
            foreach (var sToRemove in sectionsToRemove)
            {
                plan.PlanAssSections.Remove(sToRemove);
            }

            // 2. MISE À JOUR ET AJOUT
            foreach (var secDto in sectionsModifiees ?? new List<SectionAssEditDto>())
            {
                // On recherche TOUJOURS par Id pour éviter les collisions avec Entity Framework
                var isNewSection = !secDto.Id.HasValue || secDto.Id.Value == Guid.Empty;
                var section = isNewSection ? null : plan.PlanAssSections.FirstOrDefault(s => s.Id == secDto.Id.Value);

                if (section is null)
                {
                    section = PlanAssMapper.ConstruireNouvelleSection(planId, secDto);
                    plan.PlanAssSections.Add(section);
                    _logger.LogInformation("Nouvelle section ajoutée au plan {PlanId}", planId);
                }
                else
                {
                    section.TypeSectionId = secDto.TypeSectionId;
                    section.PeriodiciteId = secDto.PeriodiciteId;
                    section.OrdreAffiche = secDto.OrdreAffiche;
                    section.LibelleSection = secDto.LibelleSection ?? section.LibelleSection;
                    section.NormeReference = secDto.NormeReference;
                    section.NqaId = secDto.NqaId;
                    section.Notes = secDto.Notes;
                }

                // 3. SUPPRESSION des lignes retirées par le front
                var dtoLigneIds = secDto.Lignes.Where(l => l.Id.HasValue).Select(l => l.Id.Value).ToList();
                var lignesToRemove = section.PlanAssLignes.Where(l => !dtoLigneIds.Contains(l.Id)).ToList();
                foreach (var lToRemove in lignesToRemove)
                {
                    section.PlanAssLignes.Remove(lToRemove);
                }

                // 4. MISE À JOUR ET AJOUT des lignes
                foreach (var ligneDto in secDto.Lignes ?? new List<LigneAssEditDto>())
                {
                    var isNewLigne = !ligneDto.Id.HasValue || ligneDto.Id.Value == Guid.Empty;
                    var ligne = isNewLigne ? null : section.PlanAssLignes.FirstOrDefault(l => l.Id == ligneDto.Id.Value);

                    if (ligne is null)
                    {
                        ligne = PlanAssMapper.ConstruireNouvelleLigne(planId, section.Id, ligneDto);
                        section.PlanAssLignes.Add(ligne);
                        _logger.LogInformation("Nouvelle ligne ajoutée à la section {SectionId}", section.Id);
                    }
                    else
                    {
                        PlanAssMapper.MettreAJourEntiteLigne(ligne, ligneDto);
                        _logger.LogInformation("Ligne mise à jour: {LigneId}", ligne.Id);
                    }
                }
            }

            // Activation + archivage de l'ancienne version
            if (plan.Statut == StatutsPlan.Brouillon)
            {
                plan.Statut = StatutsPlan.Actif;
                plan.DateApplication = DateOnly.FromDateTime(DateTime.UtcNow);

                // On utilise les bonnes méthodes du repository
                var ancienPlanActif = plan.EstModele
                    ? await _repository.GetPlanActifMaitreAsync(plan.OperationCode, plan.TypeRobinetCode)
                    : await _repository.GetPlanActifExceptionAsync(plan.OperationCode, plan.TypeRobinetCode, plan.CodeArticleSage!);

                if (ancienPlanActif is not null && ancienPlanActif.Id != plan.Id)
                {
                    ancienPlanActif.Statut = StatutsPlan.Archive;
                    ancienPlanActif.ModifiePar = Truncate20(plan.ModifiePar ?? "SYSTEM");
                    ancienPlanActif.ModifieLe = DateTime.UtcNow;
                    ancienPlanActif.CommentaireVersion = $"Archivé automatiquement suite à l'activation V{plan.Version}";
                }
            }

            await _repository.SaveChangesAsync();

            _logger.LogInformation("Mise à jour du plan {PlanId} terminée avec succès. Nouveau statut: {Statut}",
                planId, plan.Statut);
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
        _logger.LogInformation("Changement de statut du plan {PlanId} vers {NouveauStatut}", planId, request.NouveauStatut);

        try
        {
            var plan = await _repository.GetPlanAvecRelationsAsync(planId);
            PlanAssSpecification.ValidatePlanExists(plan, planId);

            var ancienStatut = plan!.Statut;
            plan.Statut = request.NouveauStatut;
            plan.ModifiePar = Truncate20(modifiePar);
            plan.ModifieLe = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(request.Motif))
                plan.CommentaireVersion = request.Motif;

            await _repository.SaveChangesAsync();

            _logger.LogInformation("Statut du plan {PlanId} changé: {AncienStatut} → {NouveauStatut}",
                planId, ancienStatut, request.NouveauStatut);
            return true;
        }
        catch (PlanNotFoundException ex)
        {
            _logger.LogWarning(ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du changement de statut du plan {PlanId}", planId);
            throw;
        }
    }

    public async Task<Guid> ClonerExceptionDepuisMaitreAsync(CloneExceptionAssRequestDto request)
    {
        _logger.LogInformation(
            "Clonage d'exception depuis le plan maître. CodeArticle: {CodeArticle}",
            request.NouveauCodeArticleSage);

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

            _logger.LogInformation(
                "Exception clonée depuis le plan maître. Nouvel ID: {PlanId}, Article: {CodeArticle}",
                planDuplique.Id, request.NouveauCodeArticleSage);

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
        _logger.LogInformation(
            "Création d'une nouvelle version du plan {PlanId}. Motif: {Motif}",
            request.AncienId, request.MotifModification);

        try
        {
            var planSource = await _repository.GetPlanAvecRelationsAsync(request.AncienId);
            PlanAssSpecification.ValidatePlanExists(planSource, request.AncienId);

            var planDuplique = PlanAssMapper.DupliquerEntitePlan(
                planSource!,
                estModele: planSource!.EstModele,
                nouveauCodeArticle: planSource!.CodeArticleSage,
                nouvelleDesig: planSource!.Designation,
                creePar: request.CreePar,
                motif: request.MotifModification);

            await _repository.AddPlanAsync(planDuplique);
            await _repository.SaveChangesAsync();

            _logger.LogInformation(
                "Nouvelle version du plan créée. ID Original: {PlanSourceId}, Nouvel ID: {PlanId}, Version: {Version}",
                request.AncienId, planDuplique.Id, planDuplique.Version);

            return planDuplique.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création d'une nouvelle version du plan");
            throw;
        }
    }

    private static string Truncate20(string input)
    {
        return input?.Length > 20 ? input.Substring(0, 20) : input ?? string.Empty;
    }
}