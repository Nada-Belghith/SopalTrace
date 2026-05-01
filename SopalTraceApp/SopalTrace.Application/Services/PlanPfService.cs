using SopalTrace.Application.DTOs.QualityPlans.PlanProduitFini;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Mappers;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Application.Services;

/// <summary>
/// Service de gestion des Plans Produit Fini (Plans Génériques).
/// Plus de logique de BROUILLON ici (directement ACTIF ou ARCHIVE).
/// </summary>
public class PlanPfService : BasePlanLifecycleService<PlanPfEntete, CreatePlanPfRequestDto, UpdatePlanPfRequestDto>, IPlanPfService
{
    private readonly IPlanPfRepository _repository;

    public PlanPfService(IUnitOfWork unitOfWork, IPlanPfRepository repository) : base(unitOfWork)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    // ==================== ABSTRACT IMPLEMENTATIONS ====================

    protected override async Task<PlanPfEntete?> ObtenirEntiteAsync(Guid id)
        => await _repository.GetPlanByIdAsync(id);

    protected override async Task<PlanPfEntete> CreerEntiteAsync(CreatePlanPfRequestDto dto, string user)
    {
        var plan = new PlanPfEntete
        {
            Id = Guid.NewGuid(),
            TypeRobinetCode = dto.TypeRobinetCode,
            Designation = dto.Designation,
            DateApplication = DateOnly.FromDateTime(DateTime.UtcNow),
            CreePar = user,
            CreeLe = DateTime.UtcNow,
            CommentaireVersion = dto.CommentaireVersion,
            Remarques = dto.Remarques,
            LegendeMoyens = dto.LegendeMoyens,
            PlanPfSections = new List<PlanPfSection>()
        };

        if (dto.Sections != null && dto.Sections.Any())
            PlanPfMapper.MettreAJourArchitectureComplete(plan, dto.Sections);

        return await Task.FromResult(plan);
    }

    protected override Task ApplierMiseAJourDraftAsync(PlanPfEntete plan, UpdatePlanPfRequestDto dto, string user)
        => throw new NotSupportedException("Les plans PF n'utilisent plus la logique de brouillon.");

    protected override async Task PersisterEntiteAsync(PlanPfEntete plan)
        => await _repository.AddPlanAsync(plan);

    protected override async Task<int> CalculerNouvelleVersionAsync(PlanPfEntete plan)
    {
        var derniere = await _repository.GetDerniereVersionPlanAsync(plan.TypeRobinetCode);
        return derniere + 1;
    }

    protected override async Task<PlanPfEntete> CreerNouvelleVersionEntiteAsync(
        PlanPfEntete ancienPlan,
        UpdatePlanPfRequestDto dto,
        int nouvelleVersion,
        string user)
    {
        var nouveauPlan = PlanPfMapper.CreerNouvelleVersionEntite(
            ancienPlan,
            new NouvelleVersionPfRequestDto
            {
                AncienId = ancienPlan.Id,
                ModifiePar = user,
                MotifModification = dto.CommentaireVersion,
                Remarques = dto.Remarques,
                LegendeMoyens = dto.LegendeMoyens,
                Sections = dto.Sections
            },
            user,
            nouvelleVersion);

        if (dto.Sections != null && dto.Sections.Any())
            PlanPfMapper.MettreAJourArchitectureComplete(nouveauPlan, dto.Sections, forceNewIds: true);

        return await Task.FromResult(nouveauPlan);
    }

    protected override Task<PlanPfEntete?> ObtenirBrouillonExistantAsync(CreatePlanPfRequestDto dto)
        => Task.FromResult<PlanPfEntete?>(null);

    // ==================== PUBLIC API (LOGIQUE DIRECTE) ====================

    public async Task<List<PlanPfEnteteDto>> GetGenericPlansAsync()
    {
        var plans = await _repository.GetGenericPlansAsync();
        return plans.Select(PlanPfMapper.MapVersDto).ToList();
    }

    public async Task<PlanPfEnteteDto?> GetPlanByIdAsync(Guid id)
    {
        var plan = await ObtenirEntiteAsync(id);
        return plan == null ? null : PlanPfMapper.MapVersDto(plan);
    }

    public async Task<Guid> CreateGenericPlanAsync(CreatePlanPfRequestDto dto, string creePar)
    {
        // 1. Archiver l'ancien
        await ArchiverPlanActifExistantAsync(dto.TypeRobinetCode, creePar);

        // 2. Créer le nouveau en ACTIF
        var plan = await CreerEntiteAsync(dto, creePar);
        plan.Statut = StatutsPlan.Actif;
        plan.Version = await CalculerNouvelleVersionAsync(plan);

        await PersisterEntiteAsync(plan);
        await _unitOfWork.CommitAsync();

        return plan.Id;
    }

    public Task UpdatePlanAsync(Guid id, List<SectionPfEditDto> sectionsDto, string modifiePar, string? remarques = null, string? legendeMoyens = null)
        => throw new InvalidOperationException("La mise à jour de brouillon n'est pas supportée. Créez une nouvelle version.");

    public async Task<Guid> CreerNouvelleVersionAsync(NouvelleVersionPfRequestDto request)
    {
        var ancienPlan = await ObtenirEntiteAsync(request.AncienId);
        if (ancienPlan == null) throw new KeyNotFoundException("Plan source introuvable.");

        var user = SecuriserNomAuteur(request.ModifiePar);

        // Archiver l'ancien
        if (ancienPlan.Statut == StatutsPlan.Actif)
        {
            ancienPlan.Statut = StatutsPlan.Archive;
            ancienPlan.ModifieLe = DateTime.UtcNow;
            ancienPlan.ModifiePar = user;
        }

        var nouvelleVersion = await CalculerNouvelleVersionAsync(ancienPlan);
        var dto = new UpdatePlanPfRequestDto
        {
            Sections = request.Sections,
            CommentaireVersion = request.MotifModification,
            Remarques = request.Remarques,
            LegendeMoyens = request.LegendeMoyens
        };

        var nouveauPlan = await CreerNouvelleVersionEntiteAsync(ancienPlan, dto, nouvelleVersion, user);
        nouveauPlan.Statut = StatutsPlan.Actif;

        await PersisterEntiteAsync(nouveauPlan);
        await _unitOfWork.CommitAsync();

        return nouveauPlan.Id;
    }

    public async Task<Guid> RestaurerPlanArchiveAsync(RestaurerPfRequestDto request)
    {
        var planArchive = await ObtenirEntiteAsync(request.PlanArchiveId);
        if (planArchive == null) throw new KeyNotFoundException("Plan introuvable.");

        var user = SecuriserNomAuteur(request.RestaurePar);

        // Archiver l'actif actuel
        await ArchiverPlanActifExistantAsync(planArchive.TypeRobinetCode, user);

        var nouvelleVersion = await CalculerNouvelleVersionAsync(planArchive);
        var dto = new UpdatePlanPfRequestDto { CommentaireVersion = $"[Restauré] {request.MotifRestoration}" };
        
        var nouveauPlan = await CreerNouvelleVersionEntiteAsync(planArchive, dto, nouvelleVersion, user);
        nouveauPlan.Statut = StatutsPlan.Actif;

        await PersisterEntiteAsync(nouveauPlan);
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

    private async Task ArchiverPlanActifExistantAsync(string typeRobinetCode, string user)
    {
        var plansActifs = await _repository.GetActivePlansByTypeRobinetAsync(typeRobinetCode);
        foreach (var p in plansActifs)
        {
            p.Statut = StatutsPlan.Archive;
            p.ModifieLe = DateTime.UtcNow;
            p.ModifiePar = user;
        }
    }
}
