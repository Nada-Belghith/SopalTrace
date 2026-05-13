using FluentValidation;
using Microsoft.Extensions.Logging;
using SopalTrace.Application.DTOs.QualityPlans.PlanFabrication;
using SopalTrace.Application.DTOs.QualityPlans.PlanAssemblage;
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
/// Service de gestion des Plans de Fabrication.
/// Aligné sur IPlanFabricationService.
/// </summary>
public class PlanFabricationService : IPlanFabricationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPlanFabricationRepository _repository;
    private readonly IPlanAssRepository _assRepository;
    private readonly IPlanAssService _assService;
    private readonly IValidator<CreatePlanRequestDto> _createPlanValidator;
    private readonly IValidator<ClonePlanRequestDto> _clonePlanValidator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<PlanFabricationService> _logger;
    private readonly IPlanArchiverService _planArchiverService;

    public PlanFabricationService(
        IUnitOfWork unitOfWork,
        IPlanFabricationRepository repository,
        IPlanAssRepository assRepository,
        IPlanAssService assService,
        IValidator<CreatePlanRequestDto> createPlanValidator,
        IValidator<ClonePlanRequestDto> clonePlanValidator,
        ICurrentUserService currentUserService,
        ILogger<PlanFabricationService> logger,
        IPlanArchiverService planArchiverService)
    {
        _unitOfWork = unitOfWork;
        _repository = repository;
        _assRepository = assRepository;
        _assService = assService;
        _createPlanValidator = createPlanValidator;
        _clonePlanValidator = clonePlanValidator;
        _currentUserService = currentUserService;
        _logger = logger;
        _planArchiverService = planArchiverService;
    }

    private async Task<int> CalculerNouvelleVersionAsync(string? codeArticleSage, string? operationCode)
    {
        return (await _repository.GetDerniereVersionPlanAsync(codeArticleSage, operationCode)) + 1;
    }

    private bool IsAssemblage(string? operationCode, string? natureComposantCode, string? typeRobinetCode)
    {
        if (operationCode == "ASS") return true;
        
        var nat = natureComposantCode?.Trim().ToUpper();
        if (nat == "PISTON" || nat == "PF") return true;

        var type = typeRobinetCode?.Trim().ToUpper();
        if (type == "PISTON") return true;

        return false;
    }

    public async Task<Guid> InstancierPlanDepuisModeleAsync(CreatePlanRequestDto request)
    {
        await ValidationHelper.ValidateAndThrowAsync(_createPlanValidator, request, "CreatePlanRequest");

        var user = _currentUserService.UserInfo;

        // 1. Déterminer si on doit router vers l'Assemblage (Logique spécifique Sopal)
        if (IsAssemblage(request.OperationCode, request.NatureComposantCode, null))
        {
            var assRequest = new CreatePlanAssRequestDto
            {
                OperationCode = request.OperationCode,
                NatureComposantCode = request.NatureComposantCode,
                PosteCode = request.PosteCode,
                FamilleCode = MapperHelper.NullIfEmpty(request.FamilleCode),
                Code = request.Nom ?? $"PC-{request.CodeArticleSage}",
                EstModele = false,
                CodeArticleSage = request.CodeArticleSage,
                Nom = request.Nom ?? $"PC-{request.CodeArticleSage}",
                LegendeMoyens = request.LegendeMoyens,
                Remarques = request.Remarques,
                Sections = new List<SectionAssEditDto>() // On ne gère pas encore l'import de sections vers l'assemblage ici
            };

            return await _assService.CreerPlanAsync(assRequest, user);
        }

        // 2. Récupérer l'article (et sa nature)
        var articleSage = await _repository.GetArticleItmAsync(request.CodeArticleSage);
        if (articleSage == null) throw new ArticleSageIntrouvableException(request.CodeArticleSage);
        
        var designationSage = articleSage.Designation;

        // 2bis. Validation de la Gamme Opératoire (NatureComposant <-> Operation)
        var estValide = await _repository.IsOperationValidePourNatureAsync(articleSage.NatureComposantCode ?? "", request.OperationCode);

        if (!estValide)
        {
            throw new ValidationException($"L'opération '{request.OperationCode}' n'est pas autorisée pour un article de nature '{articleSage.NatureComposantCode}'.");
        }

        var modeleSourceId = request.ModeleSourceId.HasValue && request.ModeleSourceId.Value != Guid.Empty
            ? request.ModeleSourceId
            : null;

        // 3. Vérifier s'il existe déjà un brouillon récent (On saute si import avec sections)
        if (request.Sections == null || !request.Sections.Any())
        {
            var brouillonExistant = await _repository.GetBrouillonLePlusRecentAsync(request.CodeArticleSage, modeleSourceId, request.OperationCode);
            if (brouillonExistant != null) return brouillonExistant.Id;
        }

        // 4. Archiver l'ancien plan actif pour cet article/opération avant de créer le nouveau
        await _planArchiverService.ArchivePlanFabricationActifAsync(request.CodeArticleSage, request.OperationCode, user);

        PlanFabEntete nouveauPlan;

        // 5. Instanciation du Header
        if (modeleSourceId.HasValue)
        {
            var modeleSource = await _repository.GetModeleActifAvecRelationsAsync(modeleSourceId.Value);
            if (modeleSource == null) throw new ModeleIntrouvableException(modeleSourceId.Value);

            // Sécurité : Pas d'instanciation directe sur un modèle générique
            if (modeleSource.NatureComposantCodeNavigation?.EstGenerique == true)
                throw new ModeleGeneriqueException(modeleSourceId.Value);

            nouveauPlan = PlanFabricationMapper.ConstruireEntitePlanAPartirDeModele(modeleSource, request, designationSage);
        }
        else
        {
            nouveauPlan = PlanFabricationMapper.ConstruireEntitePlanVierge(request, designationSage);
        }

        // 6. Gestion de la version et du nom
        var prochaineVersion = await CalculerNouvelleVersionAsync(request.CodeArticleSage, request.OperationCode);
        nouveauPlan.Version = prochaineVersion;
        nouveauPlan.CreePar = user;
        nouveauPlan.Statut = StatutsPlan.Actif; // Création directe en mode ACTIF

        if (string.IsNullOrWhiteSpace(request.Nom))
        {
            nouveauPlan.Nom = prochaineVersion == 0 
                ? $"PC-{request.CodeArticleSage}" 
                : $"PC-{request.CodeArticleSage}-V{prochaineVersion}";
        }
        else
        {
            nouveauPlan.Nom = request.Nom;
        }

        nouveauPlan.Remarques = request.Remarques;
        nouveauPlan.LegendeMoyens = request.LegendeMoyens;

        // 7. Persistence immédiate des sections (Important pour Import Excel)
        if (request.Sections != null && request.Sections.Any())
        {
            await MettreAJourSectionsAsync(nouveauPlan, request.Sections, user);
            await SmartDictionaryPassAsync(nouveauPlan);
        }

        // 8. Sauvegarde finale
        try
        {
            await _repository.AddPlanAsync(nouveauPlan);
            await _unitOfWork.CommitAsync();
        }
        catch (Exception)
        {
            // Tentative de récupération en cas de conflit de concurrence
            var draft = await _repository.GetBrouillonLePlusRecentAsync(request.CodeArticleSage, modeleSourceId, request.OperationCode);
            if (draft != null) return draft.Id;
            throw;
        }

        return nouveauPlan.Id;
    }

    public async Task<PlanResponseDto> GetPlanByIdAsync(Guid planId)
    {
        var assPlan = await _assRepository.GetPlanAvecRelationsAsync(planId);
        if (assPlan != null)
        {
            // Si on trouve dans l'assemblage, on mappe
            // On le mappe vers PlanResponseDto pour l'UI de fabrication
            return new PlanResponseDto
            {
                Id = assPlan.Id,
                OperationCode = assPlan.OperationCode,
                CodeArticleSage = string.Empty, // Pas de colonne dédiée en Assemblage
                Designation = assPlan.Designation,
                Nom = assPlan.Designation,
                PosteCode = assPlan.PosteCode,
                Version = assPlan.Version,
                Statut = assPlan.Statut,
                CreePar = assPlan.CreePar,
                CreeLe = assPlan.CreeLe,
                ModifiePar = assPlan.ModifiePar,
                ModifieLe = assPlan.ModifieLe,
                LegendeMoyens = assPlan.LegendeMoyens,
                Remarques = assPlan.Remarques,
                // Mappage minimal nécessaire pour que le frontend puisse l'afficher/éditer
                Sections = assPlan.PlanAssSections.Select(s => new PlanSectionResponseDto {
                    Id = s.Id,
                    OrdreAffiche = s.OrdreAffiche,
                    LibelleSection = s.LibelleSection,
                    FrequenceLibelle = s.PeriodiciteId.HasValue ? "Périodique" : "Sans",
                    RegleEchantillonnageId = s.RegleEchantillonnageId,
                    RegleEchantillonnageLibelle = s.RegleEchantillonnage?.Libelle ?? string.Empty,
                    Lignes = s.PlanAssLignes.Select(l => new PlanLigneResponseDto {
                        Id = l.Id,
                        OrdreAffiche = l.OrdreAffiche,
                        TypeCaracteristiqueId = l.TypeCaracteristiqueId ?? Guid.Empty,
                        LibelleAffiche = l.LibelleAffiche,
                        TypeControleId = l.TypeControleId ?? Guid.Empty,
                        MoyenControleId = l.MoyenControleId,
                        InstrumentCode = l.InstrumentCode,
                        PeriodiciteId = s.PeriodiciteId,
                        LimiteSpecTexte = l.LimiteSpecTexte,
                        Observations = l.Observations,
                        Instruction = l.Instruction,
                        EstCritique = l.EstCritique
                    }).ToList()
                }).ToList()
            };
        }

        var plan = await _repository.GetPlanAvecRelationsAsync(planId);
        if (plan == null) throw new KeyNotFoundException("Plan introuvable.");
        return PlanFabricationMapper.MapperEntitePlanVersDto(plan);
    }

    public async Task<IReadOnlyList<PlanResponseDto>> GetPlansByFiltersAsync(string? typeRobinetCode, string? natureComposantCode, string? operationCode, string? posteCode = null)
    {
        var plans = await _repository.GetPlansParFiltresAsync(natureComposantCode, operationCode);
        return plans.Select(PlanFabricationMapper.MapperEntitePlanVersDto).ToList();
    }

    public async Task<Guid> CreerNouvelleVersionPlanAsync(NouvelleVersionRequestDto request)
    {
        var ancienPlan = await _repository.GetPlanAvecRelationsAsync(request.AncienId);
        if (ancienPlan == null) throw new KeyNotFoundException("Plan source introuvable.");

        var user = _currentUserService.UserInfo;

        // 1. Archiver TOUT plan actif existant pour cet article/opération (y compris l'ancien plan source s'il était actif)
        await _planArchiverService.ArchivePlanFabricationActifAsync(ancienPlan.CodeArticleSage, ancienPlan.OperationCode, user);

        var nouvelleVersion = await CalculerNouvelleVersionAsync(ancienPlan.CodeArticleSage, ancienPlan.OperationCode);
        var nouveauPlan = PlanFabricationMapper.DupliquerEntitePlan(ancienPlan, ancienPlan.CodeArticleSage, ancienPlan.Designation ?? string.Empty, user, request.MotifModification);
        
        nouveauPlan.Remarques = request.Remarques;
        nouveauPlan.LegendeMoyens = request.LegendeMoyens;
        nouveauPlan.Version = nouvelleVersion;
        nouveauPlan.Statut = StatutsPlan.Actif;

        if (request.SectionsModifiees != null && request.SectionsModifiees.Any())
        {
            await MettreAJourSectionsAsync(nouveauPlan, request.SectionsModifiees, user);
        }

        await SmartDictionaryPassAsync(nouveauPlan);

        await _repository.AddPlanAsync(nouveauPlan);
        await _unitOfWork.CommitAsync();

        return nouveauPlan.Id;
    }

    public async Task<Guid> ClonerPlanPourNouvelArticleAsync(ClonePlanRequestDto request)
    {
        await ValidationHelper.ValidateAndThrowAsync(_clonePlanValidator, request, "ClonePlanRequest");

        var user = _currentUserService.UserInfo;
        var sourcePlan = await _repository.GetPlanAvecRelationsAsync(request.PlanExistantId);
        if (sourcePlan == null) throw new KeyNotFoundException("Plan source introuvable.");

        // Archiver l'éventuel plan déjà existant pour l'article cible
        await _planArchiverService.ArchivePlanFabricationActifAsync(request.NouveauCodeArticleSage, sourcePlan.OperationCode, user);

        var desig = await _repository.GetDesignationArticleSageAsync(request.NouveauCodeArticleSage);
        var nouveauPlan = PlanFabricationMapper.DupliquerEntitePlan(sourcePlan, request.NouveauCodeArticleSage, desig ?? request.NouvelleDesignation, user);
        nouveauPlan.Statut = StatutsPlan.Actif;
        var nouvelleVersion = await CalculerNouvelleVersionAsync(request.NouveauCodeArticleSage, sourcePlan.OperationCode);
        nouveauPlan.Version = nouvelleVersion;

        await _repository.AddPlanAsync(nouveauPlan);
        await _unitOfWork.CommitAsync();

        return nouveauPlan.Id;
    }

    public async Task<Guid> RestaurerPlanArchiveAsync(RestaurerPlanRequestDto request)
    {
        var planArchive = await _repository.GetPlanAvecRelationsAsync(request.PlanArchiveId);
        if (planArchive == null) throw new KeyNotFoundException("Plan introuvable.");

        var user = _currentUserService.UserInfo;

        // Archiver l'actif actuel
        await _planArchiverService.ArchivePlanFabricationActifAsync(planArchive.CodeArticleSage, planArchive.OperationCode, user);

        var nouvelleVersion = await CalculerNouvelleVersionAsync(planArchive.CodeArticleSage, planArchive.OperationCode);
        var nouveauPlan = PlanFabricationMapper.DupliquerEntitePlan(planArchive, planArchive.CodeArticleSage, planArchive.Designation ?? string.Empty, user, $"[Restauré] {request.MotifRestoration}");
        nouveauPlan.Statut = StatutsPlan.Actif;
        nouveauPlan.Version = nouvelleVersion;

        await SmartDictionaryPassAsync(nouveauPlan);

        await _repository.AddPlanAsync(nouveauPlan);
        await _unitOfWork.CommitAsync();

        return nouveauPlan.Id;
    }

    public async Task<bool> MettreAJourValeursPlanAsync(Guid planId, List<SectionEditDto> sectionsModifiees, string? legendeMoyens = null, string? remarques = null, bool finaliser = true, string? nom = null, string? modifiePar = null)
    {
        var plan = await _repository.GetPlanCompletPourMiseAJourAsync(planId);
        if (plan == null) return false;

        plan.ModifiePar = _currentUserService.UserInfo;
        plan.ModifieLe = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(nom)) plan.Nom = nom;
        if (legendeMoyens is not null) plan.LegendeMoyens = string.IsNullOrWhiteSpace(legendeMoyens) ? null : legendeMoyens;
        if (remarques is not null) plan.Remarques = string.IsNullOrWhiteSpace(remarques) ? null : remarques;

        await MettreAJourSectionsAsync(plan, sectionsModifiees, plan.ModifiePar);

        await SmartDictionaryPassAsync(plan);

        await _unitOfWork.CommitAsync();
        return true;
    }

    public async Task<bool> ChangerStatutPlanAsync(Guid planId, ChangePlanStatusRequestDto request, string modifiePar)
    {
        var plan = await _repository.GetPlanByIdAsync(planId);
        if (plan == null) return false;

        plan.Statut = request.NouveauStatut;
        plan.ModifiePar = _currentUserService.UserInfo;
        plan.ModifieLe = DateTime.UtcNow;

        await _unitOfWork.CommitAsync();
        return true;
    }

    public async Task<bool> SupprimerBrouillonAsync(Guid planId)
    {
        var plan = await _repository.GetPlanByIdAsync(planId);
        if (plan == null) return false;
        if (plan.Statut != StatutsPlan.Brouillon) return false;

        _repository.Delete(plan);
        await _unitOfWork.CommitAsync();
        return true;
    }

    private Task MettreAJourSectionsAsync(PlanFabEntete plan, List<SectionEditDto> sectionsModifiees, string user)
    {
        SectionUpdateHelper.UpdateSections(
            plan.PlanFabSections,
            sectionsModifiees,
            deleteSection: section => _repository.DeleteSection(section),
            deleteLine: line => _repository.DeleteLigne(line),
            getSectionDtoId: dto => dto.Id,
            getLineDtoId: dto => dto.Id,
            getSectionId: section => section.Id,
            getLineId: line => line.Id,
            createSection: dto => PlanFabricationMapper.ConstruireNouvelleSectionPlan(plan.Id, dto),
                updateSection: (section, dto) =>
                {
                    section.OrdreAffiche = dto.OrdreAffiche;
                    section.FrequenceLibelle = string.IsNullOrWhiteSpace(dto.FrequenceLibelle) ? null : dto.FrequenceLibelle;
                    section.TypeSectionId = dto.TypeSectionId;
                    section.PeriodiciteId = dto.PeriodiciteId;
                    section.RegleEchantillonnageId = dto.RegleEchantillonnageId;

                    // ✅ Reconstruction systématique du libellé complet côté serveur (Nature + Fréq + Règle)
                    section.LibelleSection = PlanFabricationMapper.ReconstruireLibelleComplet(dto.LibelleSection, null, dto.FrequenceLibelle, dto.RegleEchantillonnageLibelle);
                },
            getLines: section => section.PlanFabLignes,
            getLineDtos: dto => dto.Lignes,
            createLine: (dto, section) => PlanFabricationMapper.ConstruireNouvelleLignePlan(plan.Id, section.Id, dto),
            updateLine: (line, dto) => PlanFabricationMapper.MettreAJourEntiteLigne(line, dto)
        );

        return Task.CompletedTask;
    }

    private async Task SmartDictionaryPassAsync(PlanFabEntete plan)
    {
        var addedCaracs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var addedInstruments = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var addedMoyens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var addedRegles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var sec in plan.PlanFabSections)
        {
            // ✅ On tente de retrouver la règle existante par son libellé (sans jamais en créer une nouvelle)
            var regleLibelle = sec.RegleEchantillonnageLibelle;
            void SetRegleId(Guid? id) => sec.RegleEchantillonnageId = id;

            var lignes = sec.PlanFabLignes.Select(l => 
            {
                // Nettoyage des chaînes
                if (string.IsNullOrWhiteSpace(l.InstrumentCode)) l.InstrumentCode = null;
                if (string.IsNullOrWhiteSpace(l.LibelleAffiche)) l.LibelleAffiche = null;
                if (string.IsNullOrWhiteSpace(l.MoyenTexteLibre)) l.MoyenTexteLibre = null;

                void SetCaracId(Guid? id) { if(id.HasValue) l.TypeCaracteristiqueId = id.Value; }
                void SetMoyenId(Guid? id) => l.MoyenControleId = id;

                var caracOk = !string.IsNullOrWhiteSpace(l.LibelleAffiche) && (l.TypeCaracteristiqueId == null || l.TypeCaracteristiqueId == Guid.Empty);
                var moyenOk = !string.IsNullOrWhiteSpace(l.MoyenTexteLibre) && (l.MoyenControleId == null || l.MoyenControleId == Guid.Empty);

                return (
                    caracOk ? l.LibelleAffiche : null,
                    (Action<Guid?>)SetCaracId,
                    moyenOk ? l.MoyenTexteLibre : null,
                    (Action<Guid?>)SetMoyenId,
                    l.InstrumentCode
                );
            });

            await SmartDictionaryHelper.ResolveAndCreateMissingReferencesAsync(
                _unitOfWork.DictionnaireQualiteRepository,
                sec.RegleEchantillonnageId == null ? regleLibelle : null,
                SetRegleId,
                lignes,
                addedRegles,
                addedCaracs,
                addedMoyens,
                addedInstruments
            );
        }
    }
}
