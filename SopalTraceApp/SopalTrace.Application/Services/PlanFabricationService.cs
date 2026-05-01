using FluentValidation;
using Microsoft.Extensions.Logging;
using SopalTrace.Application.DTOs.QualityPlans.PlanFabrication;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Mappers;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Application.Services;

/// <summary>
/// Service de gestion des Plans de Fabrication.
/// Hérite de BasePlanArticleLifecycleService pour bénéficier du cycle de vie complet
/// (Draft, Activer, Archiver, Versioning, Restaurer, SauvegardeAuto) sans redondance.
/// 
/// - TCreateDto = CreatePlanRequestDto  (instanciation depuis modèle ou vierge)
/// - TUpdateDto = NouvelleVersionRequestDto  (mise à jour sections/motif)
/// </summary>
public class PlanFabricationService
    : BasePlanArticleLifecycleService<PlanFabEntete, CreatePlanRequestDto, NouvelleVersionRequestDto>,
      IPlanFabricationService
{
    private readonly IPlanFabricationRepository _repository;
    private readonly IValidator<CreatePlanRequestDto> _createPlanValidator;
    private readonly IValidator<ClonePlanRequestDto> _clonePlanValidator;
    private readonly IValidator<PlanFabEntete> _planActivationValidator;

    public PlanFabricationService(
        IUnitOfWork unitOfWork,
        IPlanFabricationRepository repository,
        IValidator<CreatePlanRequestDto> createPlanValidator,
        IValidator<ClonePlanRequestDto> clonePlanValidator,
        IValidator<PlanFabEntete> planActivationValidator)
        : base(unitOfWork)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _createPlanValidator = createPlanValidator ?? throw new ArgumentNullException(nameof(createPlanValidator));
        _clonePlanValidator = clonePlanValidator ?? throw new ArgumentNullException(nameof(clonePlanValidator));
        _planActivationValidator = planActivationValidator ?? throw new ArgumentNullException(nameof(planActivationValidator));
    }

    // ==================== ABSTRACT IMPLEMENTATIONS ====================

    protected override async Task<PlanFabEntete?> ObtenirEntiteAsync(Guid id)
        => await _repository.GetPlanByIdAsync(id);

    protected override async Task<PlanFabEntete> CreerEntiteAsync(CreatePlanRequestDto dto, string user)
    {
        var validationResult = await _createPlanValidator.ValidateAsync(dto);
        if (!validationResult.IsValid) throw new ValidationException(validationResult.Errors);

        var designationSage = await _repository.GetDesignationArticleSageAsync(dto.CodeArticleSage);
        if (string.IsNullOrEmpty(designationSage)) throw new ArticleSageIntrouvableException(dto.CodeArticleSage);

        var modeleSourceId = dto.ModeleSourceId.HasValue && dto.ModeleSourceId.Value != Guid.Empty
            ? dto.ModeleSourceId : null;

        PlanFabEntete plan;
        if (modeleSourceId.HasValue)
        {
            var modeleSource = await _repository.GetModeleActifAvecRelationsAsync(modeleSourceId.Value);
            if (modeleSource == null) throw new ModeleIntrouvableException(modeleSourceId.Value);
            if (modeleSource.NatureComposantCodeNavigation != null && modeleSource.NatureComposantCodeNavigation.EstGenerique)
                throw new ModeleGeneriqueException(modeleSourceId.Value);
            plan = PlanFabricationMapper.ConstruireEntitePlanAPartirDeModele(modeleSource, dto, designationSage);
        }
        else
        {
            plan = PlanFabricationMapper.ConstruireEntitePlanVierge(dto, designationSage);
        }

        plan.Remarques = dto.Remarques;
        plan.LegendeMoyens = dto.LegendeMoyens;
        plan.CreePar = user;
        plan.CreeLe = DateTime.UtcNow;

        return plan;
    }

    protected override async Task ApplierMiseAJourDraftAsync(PlanFabEntete plan, NouvelleVersionRequestDto dto, string user)
    {
        plan.Remarques = dto.Remarques;
        plan.LegendeMoyens = dto.LegendeMoyens;
        plan.CommentaireVersion = dto.MotifModification;
        plan.ModifiePar = user;
        plan.ModifieLe = DateTime.UtcNow;

        if (dto.SectionsModifiees != null && dto.SectionsModifiees.Any())
        {
            await MettreAJourSectionsAsync(plan, dto.SectionsModifiees, user);
        }
    }

    protected override async Task PersisterEntiteAsync(PlanFabEntete plan)
    {
        await _repository.AddPlanAsync(plan);
    }

    protected override async Task<int> CalculerNouvelleVersionAsync(PlanFabEntete plan)
        => await _repository.GetDerniereVersionPlanAsync(plan.CodeArticleSage, plan.OperationCode) + 1;

    protected override async Task<PlanFabEntete> CreerNouvelleVersionEntiteAsync(
        PlanFabEntete ancienPlan,
        NouvelleVersionRequestDto dto,
        int nouvelleVersion,
        string user)
    {
        var commentaire = dto.MotifModification;
        var nouveauPlan = PlanFabricationMapper.DupliquerEntitePlan(
            ancienPlan,
            ancienPlan.CodeArticleSage,
            ancienPlan.Designation ?? $"Copy-{ancienPlan.Id}",
            user,
            commentaire);

        nouveauPlan.Remarques = dto.Remarques;
        nouveauPlan.LegendeMoyens = dto.LegendeMoyens;
        nouveauPlan.Version = nouvelleVersion;
        nouveauPlan.Nom = string.IsNullOrWhiteSpace(nouveauPlan.Nom)
            ? (nouvelleVersion == 1 ? $"PC-{ancienPlan.CodeArticleSage}" : $"PC-{ancienPlan.CodeArticleSage}-V{nouvelleVersion}")
            : ModeleFabricationMapper.IncrementerSuffixeVersion(nouveauPlan.Nom, nouvelleVersion);

        return await Task.FromResult(nouveauPlan);
    }

    protected override async Task<PlanFabEntete?> ObtenirBrouillonExistantAsync(CreatePlanRequestDto dto)
    {
        var modeleSourceId = dto.ModeleSourceId.HasValue && dto.ModeleSourceId.Value != Guid.Empty
            ? dto.ModeleSourceId : null;
        return await _repository.GetBrouillonLePlusRecentAsync(dto.CodeArticleSage, modeleSourceId, dto.OperationCode);
    }

    // ==================== HOOK: Archiver plan actif avant activation ====================

    protected override async Task HandleVersioningBeforeActivationAsync(PlanFabEntete plan, string user)
    {
        await ArchiverPlanActifExistantAsync(plan.CodeArticleSage, plan.OperationCode, user);
        await _unitOfWork.CommitAsync();
    }

    // ==================== PUBLIC API (métier spécifique FAB) ====================

    public async Task<Guid> InstancierPlanDepuisModeleAsync(CreatePlanRequestDto request)
    {
        // On délègue maintenant proprement à la classe de base qui gère :
        // - La détection de brouillon existant (via ObtenirBrouillonExistantAsync)
        // - Les conflits de concurrence
        // - L'initialisation du statut
        return await CreerBrouillonAsync(request, "SYSTEM");
    }

    public async Task<bool> SupprimerBrouillonAsync(Guid planId)
    {
        var plan = await _repository.GetPlanByIdAsync(planId);
        if (plan == null) return false;
        if (plan.Statut != StatutsPlan.Brouillon)
            throw new InvalidOperationException("Impossible de supprimer ce plan : seul un BROUILLON peut être détruit physiquement.");

        _repository.Delete(plan);
        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<PlanResponseDto> GetPlanByIdAsync(Guid planId)
    {
        var plan = await _repository.GetPlanAvecRelationsAsync(planId);
        if (plan == null) throw new PlanIntrouvableException(planId);
        return PlanFabricationMapper.MapperEntitePlanVersDto(plan);
    }

    public async Task<bool> MettreAJourValeursPlanAsync(Guid planId, List<SectionEditDto> sectionsModifiees, string? legendeMoyens = null, string? remarques = null, bool finaliser = true, string? nom = null, string? modifiePar = null)
    {
        var plan = await _repository.GetPlanCompletPourMiseAJourAsync(planId);
        if (plan == null) return false;

        plan.ModifiePar = modifiePar ?? "SYSTEM";
        plan.ModifieLe = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(nom)) plan.Nom = nom;
        if (legendeMoyens is not null) plan.LegendeMoyens = string.IsNullOrWhiteSpace(legendeMoyens) ? null : legendeMoyens;
        if (remarques is not null) plan.Remarques = string.IsNullOrWhiteSpace(remarques) ? null : remarques;

        await MettreAJourSectionsAsync(plan, sectionsModifiees, plan.ModifiePar);

        if (plan.Statut == StatutsPlan.Brouillon && finaliser)
        {
            await ValiderPlanPourActivationAsync(plan);
            await ArchiverPlanActifExistantAsync(plan.CodeArticleSage, plan.OperationCode, plan.ModifiePar ?? "SYSTEM");
            await _repository.SaveChangesAsync();

            plan.Statut = StatutsPlan.Actif;
            plan.DateApplication = DateOnly.FromDateTime(DateTime.UtcNow);

            if (string.IsNullOrWhiteSpace(plan.Nom))
                plan.Nom = plan.Version == 1 ? $"PC-{plan.CodeArticleSage}" : $"PC-{plan.CodeArticleSage}-V{plan.Version}";
        }

        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ChangerStatutPlanAsync(Guid planId, ChangePlanStatusRequestDto request, string modifiePar)
    {
        var plan = await _repository.GetPlanByIdAsync(planId);
        if (plan == null) throw new PlanIntrouvableException(planId);
        if (plan.Statut == request.NouveauStatut) return false;

        plan.Statut = request.NouveauStatut;
        plan.ModifiePar = SecuriserNomAuteur(modifiePar);
        plan.ModifieLe = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(request.Motif)) plan.CommentaireVersion = request.Motif;

        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<Guid> ClonerPlanPourNouvelArticleAsync(ClonePlanRequestDto request)
    {
        var validationResult = await _clonePlanValidator.ValidateAsync(request);
        if (!validationResult.IsValid) throw new ValidationException(validationResult.Errors);

        var designationSage = await _repository.GetDesignationArticleSageAsync(request.NouveauCodeArticleSage);
        if (string.IsNullOrEmpty(designationSage)) throw new ArticleSageIntrouvableException(request.NouveauCodeArticleSage);

        var planSource = await _repository.GetPlanAvecRelationsAsync(request.PlanExistantId);
        if (planSource == null) throw new PlanIntrouvableException(request.PlanExistantId);

        var brouillonExistant = await _repository.GetBrouillonLePlusRecentAsync(request.NouveauCodeArticleSage, planSource.ModeleSourceId, planSource.OperationCode);
        if (brouillonExistant != null) return brouillonExistant.Id;

        var planClone = PlanFabricationMapper.DupliquerEntitePlan(planSource, request.NouveauCodeArticleSage, designationSage, SecuriserNomAuteur(request.CreePar));

        var prochaineVersion = await _repository.GetDerniereVersionPlanAsync(request.NouveauCodeArticleSage, planSource.OperationCode) + 1;
        planClone.Version = prochaineVersion;
        planClone.Nom = prochaineVersion == 1 ? $"PC-{request.NouveauCodeArticleSage}" : $"PC-{request.NouveauCodeArticleSage}-V{prochaineVersion}";

        try
        {
            await _repository.AddPlanAsync(planClone);
            await _repository.SaveChangesAsync();
        }
        catch (ConflitConcurrenceException)
        {
            var draft = await _repository.GetBrouillonLePlusRecentAsync(request.NouveauCodeArticleSage, planSource.ModeleSourceId, planSource.OperationCode);
            if (draft != null) return draft.Id;
            throw;
        }

        return planClone.Id;
    }

    public async Task<Guid> CreerNouvelleVersionPlanAsync(NouvelleVersionRequestDto request)
    {
        var ancienPlan = await _repository.GetPlanAvecRelationsAsync(request.AncienId);
        if (ancienPlan == null) throw new PlanIntrouvableException(request.AncienId);
        if (ancienPlan.Statut == StatutsPlan.Archive) throw new PlanArchiveException();
        if (ancienPlan.Statut != StatutsPlan.Actif) throw new PlanSourceNonActifException(request.AncienId);

        // Reprise silencieuse d'un brouillon existant
        var brouillonExistant = await _repository.GetBrouillonLePlusRecentAsync(ancienPlan.CodeArticleSage, ancienPlan.ModeleSourceId, ancienPlan.OperationCode);
        if (brouillonExistant != null) return brouillonExistant.Id;

        var nouvelleVersion = await CalculerNouvelleVersionAsync(ancienPlan);
        var nouveauPlan = await CreerNouvelleVersionEntiteAsync(ancienPlan, request, nouvelleVersion, SecuriserNomAuteur(request.ModifiePar));
        nouveauPlan.Statut = StatutsPlan.Brouillon;

        try
        {
            await _repository.AddPlanAsync(nouveauPlan);
            await _repository.SaveChangesAsync();
        }
        catch (ConflitConcurrenceException)
        {
            var draft = await _repository.GetBrouillonLePlusRecentAsync(ancienPlan.CodeArticleSage, ancienPlan.ModeleSourceId, ancienPlan.OperationCode);
            if (draft != null) return draft.Id;
            throw;
        }

        return nouveauPlan.Id;
    }

    public async Task<Guid> RestaurerPlanArchiveAsync(RestaurerPlanRequestDto request)
    {
        var dto = new NouvelleVersionRequestDto
        {
            AncienId = request.PlanArchiveId,
            ModifiePar = request.RestaurePar,
            MotifModification = $"[Restauré depuis archive] {request.MotifRestoration}"
        };

        // Délègue à BasePlanArticleLifecycleService.RestaurerPlanArchiveAsync
        return await RestaurerPlanArchiveAsync(request.PlanArchiveId, dto, request.RestaurePar);
    }

    public async Task<IReadOnlyList<PlanResponseDto>> GetPlansByFiltersAsync(string? typeRobinetCode, string? natureComposantCode, string? operationCode)
    {
        var plans = await _repository.GetPlansParFiltresAsync(typeRobinetCode, natureComposantCode, operationCode);
        return plans.Select(PlanFabricationMapper.MapperEntitePlanVersDto).ToList();
    }

    // ==================== PRIVATE HELPERS ====================

    private async Task MettreAJourSectionsAsync(PlanFabEntete plan, List<SectionEditDto> sectionsModifiees, string user)
    {
        var sectionsAconserver = new List<PlanFabSection>();

        foreach (var secDto in sectionsModifiees)
        {
            var section = secDto.Id.HasValue ? plan.PlanFabSections.FirstOrDefault(s => s.Id == secDto.Id.Value) : null;

            if (section != null)
            {
                section.OrdreAffiche = secDto.OrdreAffiche;
                section.LibelleSection = secDto.LibelleSection ?? section.LibelleSection;
                section.FrequenceLibelle = string.IsNullOrWhiteSpace(secDto.FrequenceLibelle) ? null : secDto.FrequenceLibelle;
            }
            else
            {
                section = PlanFabricationMapper.ConstruireNouvelleSectionPlan(plan.Id, secDto);
                plan.PlanFabSections.Add(section);
            }

            sectionsAconserver.Add(section);

            var lignesAconserver = new List<PlanFabLigne>();
            foreach (var ligDto in secDto.Lignes)
            {
                var ligne = ligDto.Id.HasValue ? section.PlanFabLignes.FirstOrDefault(l => l.Id == ligDto.Id.Value) : null;

                if (ligne != null)
                {
                    PlanFabricationMapper.MettreAJourEntiteLigne(ligne, ligDto);
                    ligne.OrdreAffiche = ligDto.OrdreAffiche;
                }
                else
                {
                    ligne = PlanFabricationMapper.ConstruireNouvelleLignePlan(plan.Id, section.Id, ligDto);
                    section.PlanFabLignes.Add(ligne);
                }
                lignesAconserver.Add(ligne);
            }

            var lignesASupprimer = section.PlanFabLignes.Where(l => !lignesAconserver.Contains(l)).ToList();
            foreach (var l in lignesASupprimer)
            {
                section.PlanFabLignes.Remove(l);
                _repository.DeleteLigne(l);
            }
        }

        var sectionsASupprimer = plan.PlanFabSections.Where(s => !sectionsAconserver.Contains(s)).ToList();
        foreach (var s in sectionsASupprimer)
        {
            plan.PlanFabSections.Remove(s);
            _repository.DeleteSection(s);
        }
    }

    private async Task ValiderPlanPourActivationAsync(PlanFabEntete plan)
    {
        var validationResult = await _planActivationValidator.ValidateAsync(plan);
        if (!validationResult.IsValid) throw new ValidationException(validationResult.Errors);
    }

    private async Task ArchiverPlanActifExistantAsync(string codeArticleSage, string? operationCode, string auteur, string? commentaireVersion = null)
    {
        var planActifExiste = await _repository.GetPlanActifPourArticleEtOperationAsync(codeArticleSage, operationCode);
        if (planActifExiste != null)
        {
            planActifExiste.Statut = StatutsPlan.Archive;
            planActifExiste.ModifiePar = auteur;
            planActifExiste.ModifieLe = DateTime.UtcNow;
            if (commentaireVersion != null) planActifExiste.CommentaireVersion = commentaireVersion;
        }
    }
}