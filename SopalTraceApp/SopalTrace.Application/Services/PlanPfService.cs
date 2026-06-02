using SopalTrace.Application.DTOs.QualityPlans.PlanProduitFini;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Mappers;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Application.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Application.Services;

/// <summary>
/// Service de gestion des Plans Produit Fini (Plans Génériques).
/// Aligné sur IPlanPfService.
/// </summary>
public class PlanPfService : IPlanPfService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPlanPfRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPlanArchiverService _planArchiverService;

    public PlanPfService(IUnitOfWork unitOfWork, IPlanPfRepository repository, ICurrentUserService currentUserService, IPlanArchiverService planArchiverService)
    {
        _unitOfWork = unitOfWork;
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _currentUserService = currentUserService;
        _planArchiverService = planArchiverService;
    }

    private async Task<int> CalculerNouvelleVersionAsync(string familleProduitFiniCode)
    {
        return (await _repository.GetDerniereVersionPlanAsync(familleProduitFiniCode)) + 1;
    }

    public async Task<List<PlanPfEnteteDto>> GetGenericPlansAsync()
    {
        var plans = await _repository.GetGenericPlansAsync();
        return plans.Select(PlanPfMapper.MapVersDto).ToList();
    }

    public async Task<PlanPfEnteteDto?> GetPlanByIdAsync(Guid id)
    {
        var plan = await _repository.GetPlanByIdAsync(id);
        return plan == null ? null : PlanPfMapper.MapVersDto(plan);
    }

    public async Task<Guid> CreateGenericPlanAsync(CreatePlanPfRequestDto dto, string creePar)
    {
        var user = _currentUserService.UserInfo;

        // 1. Archiver l'ancien
        await _planArchiverService.ArchivePlansPfActifsAsync(dto.FamilleProduitFiniCode ?? "", user);

        // 2. Créer le nouveau en ACTIF
        var plan = new PlanProduitFiniEntete
        {
            Id = Guid.NewGuid(),
            FamilleProduitFiniCode = dto.FamilleProduitFiniCode,
            CreePar = user,
            CreeLe = DateTime.UtcNow,
            //Remarques = dto.Remarques,
            //LegendeMoyens = dto.LegendeMoyens,
            Statut = StatutsPlan.Actif,
            PlanProduitFiniSections = new List<PlanProduitFiniSection>()
        };

        if (dto.Sections != null && dto.Sections.Any())
        {
            PlanPfMapper.MettreAJourArchitectureComplete(plan, dto.Sections);
            await SmartDictionaryPassAsync(plan);
        }

        plan.Version = await CalculerNouvelleVersionAsync(plan.FamilleProduitFiniCode ?? "");

        await _repository.AddPlanAsync(plan);
        await _unitOfWork.CommitAsync();

        return plan.Id;
    }

    public async Task<Guid> CreerNouvelleVersionAsync(NouvelleVersionPfRequestDto request)
    {
        var ancienPlan = await _repository.GetPlanByIdAsync(request.AncienId);
        if (ancienPlan == null) throw new KeyNotFoundException("Plan source introuvable.");

        var user = _currentUserService.UserInfo;

        // Archiver l'ancien
        if (ancienPlan.Statut == StatutsPlan.Actif)
        {
            ancienPlan.Statut = StatutsPlan.Archive;
            //ancienPlan.ModifieLe = DateTime.UtcNow;
            //ancienPlan.ModifiePar = user;
        }

        var nouvelleVersion = await CalculerNouvelleVersionAsync(ancienPlan.FamilleProduitFiniCode ?? "");

        var nouveauPlan = PlanPfMapper.CreerNouvelleVersionEntite(
            ancienPlan,
            request,
            user,
            nouvelleVersion);

        if (request.Sections != null && request.Sections.Any())
        {
            PlanPfMapper.MettreAJourArchitectureComplete(nouveauPlan, request.Sections, forceNewIds: true);
            await SmartDictionaryPassAsync(nouveauPlan);
        }

        nouveauPlan.Statut = StatutsPlan.Actif;

        await _repository.AddPlanAsync(nouveauPlan);
        await _unitOfWork.CommitAsync();

        return nouveauPlan.Id;
    }

    public async Task ArchiverPlanAsync(Guid id, string modifiePar)
    {
        var plan = await _repository.GetPlanByIdAsync(id);
        if (plan == null) return;

        plan.Statut = StatutsPlan.Archive;
        //plan.ModifieLe = DateTime.UtcNow;
        //plan.ModifiePar = PlanMetadataHelper.NormalizeAuthorNameWithTruncation(modifiePar);

        await _unitOfWork.CommitAsync();
    }

    public async Task<Guid> RestaurerPlanArchiveAsync(RestaurerPfRequestDto request)
    {
        var planArchive = await _repository.GetPlanByIdAsync(request.PlanArchiveId);
        if (planArchive == null) throw new KeyNotFoundException("Plan introuvable.");

        var user = _currentUserService.UserInfo;

        // Archiver l'actif actuel
        await _planArchiverService.ArchivePlansPfActifsAsync(planArchive.FamilleProduitFiniCode ?? "", user);

        var nouvelleVersion = await CalculerNouvelleVersionAsync(planArchive.FamilleProduitFiniCode ?? "");

        var requestVersion = new NouvelleVersionPfRequestDto
        { 
            AncienId = planArchive.Id,
            FamilleProduitFiniCode = planArchive.FamilleProduitFiniCode,
            ModifiePar = user,
            MotifModification = $"[Restauré] {request.MotifRestoration}",
            //Remarques = planArchive.Remarques ?? string.Empty,
            //LegendeMoyens = planArchive.LegendeMoyens ?? string.Empty
        };
        
        var nouveauPlan = PlanPfMapper.CreerNouvelleVersionEntite(planArchive, requestVersion, user, nouvelleVersion);
        nouveauPlan.Statut = StatutsPlan.Actif;

        // Copier les sections de l'ancien plan
        if (planArchive.PlanProduitFiniSections != null && planArchive.PlanProduitFiniSections.Any())
        {
            var sectionsDto = planArchive.PlanProduitFiniSections
                .OrderBy(s => s.OrdreAffiche)
                .Select(s => s.ConvertProduitFiniSectionEntityToEditableDto())
                .ToList();

            PlanPfMapper.MettreAJourArchitectureComplete(nouveauPlan, sectionsDto, forceNewIds: true);
        }

        await SmartDictionaryPassAsync(nouveauPlan);

        await _repository.AddPlanAsync(nouveauPlan);
        await _unitOfWork.CommitAsync();

        return nouveauPlan.Id;
    }

    public async Task<bool> SupprimerBrouillonAsync(Guid id)
    {
        var plan = await _repository.GetPlanByIdAsync(id);
        if (plan == null || plan.Statut != StatutsPlan.Brouillon) return false;

        _repository.DeletePlan(plan);
        await _unitOfWork.CommitAsync();
        return true;
    }

    private async Task SmartDictionaryPassAsync(PlanProduitFiniEntete plan)
    {
        var addedCaracs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var addedInstruments = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var addedMoyens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var addedRegles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var sec in plan.PlanProduitFiniSections)
        {
            var regleLibelle = sec.RegleEchantillonnageLibelle;
            void SetRegleId(Guid? id) => sec.RegleEchantillonnageId = id;

            var lignes = sec.PlanProduitFiniLignes.Select(l => 
            {
                // Nettoyage des chaînes vides
                if (string.IsNullOrWhiteSpace(l.InstrumentCode)) l.InstrumentCode = null;
                if (string.IsNullOrWhiteSpace(l.LibelleAffiche)) l.LibelleAffiche = null;
                if (string.IsNullOrWhiteSpace(l.MoyenTexteLibre)) l.MoyenTexteLibre = null;

                void SetCaracId(Guid? id) { if(id.HasValue && id.Value != Guid.Empty) l.TypeCaracteristiqueId = id.Value; }
                void SetMoyenId(Guid? id) => l.MoyenControleId = id;

                var caracOk = !string.IsNullOrWhiteSpace(l.LibelleAffiche) && l.TypeCaracteristiqueId == Guid.Empty;
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

            // Final cleanup to enforce XOR constraint on means of control
            foreach (var l in sec.PlanProduitFiniLignes)
            {
                LineCleanupHelper.CleanupPlanPfLine(l);
            }
        }
    }
}
