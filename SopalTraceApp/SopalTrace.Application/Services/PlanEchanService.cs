using FluentValidation;
using Microsoft.Extensions.Logging;
using SopalTrace.Application.DTOs.QualityPlans.PlansEchantillonnage;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Mappers;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Application.Services;

public class PlanEchanService : BasePlanLifecycleService<PlanEchantillonnageEntete, CreatePlanEchanRequestDto, UpdatePlanEchanRequestDto>, IPlanEchanService
{
    private readonly IValidator<CreatePlanEchanRequestDto> _createValidator;
    private readonly ILogger<PlanEchanService> _logger;

    public PlanEchanService(IUnitOfWork unitOfWork, IValidator<CreatePlanEchanRequestDto> createValidator, ILogger<PlanEchanService> logger)
        : base(unitOfWork)
    {
        _createValidator = createValidator;
        _logger = logger;
    }

    protected override async Task<List<string>> ValidateCreationAsync(CreatePlanEchanRequestDto dto)
    {
        var erreurs = new List<string>();
        var validationResult = await _createValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
            erreurs.AddRange(validationResult.Errors.Select(e => e.ErrorMessage));
        if (await _unitOfWork.PlanEchanRepository.ExistePlanActifAsync())
            erreurs.Add("Un profil d'échantillonnage ACTIF existe déjà.");
        return erreurs;
    }

    protected override async Task HandleVersioningBeforeActivationAsync(PlanEchantillonnageEntete plan, string user) => await Task.CompletedTask;

    /// <summary>
    /// Le plan d'échantillonnage n'a pas de cycle brouillon : il naît directement ACTIF.
    /// La contrainte SQL CK__Plan_Echa__Statu__ n'accepte pas 'BROUILLON'.
    /// </summary>
    protected override string GetStatutInitial() => StatutsPlan.Actif;

    protected override async Task<PlanEchantillonnageEntete?> ObtenirEntiteAsync(Guid id) => await _unitOfWork.PlanEchanRepository.GetPlanAvecRelationsAsync(id);

    protected override async Task<PlanEchantillonnageEntete> CreerEntiteAsync(CreatePlanEchanRequestDto dto, string user)
    {
        int nqaIdDb = dto.NqaId ?? 0;
        if (dto.ValeurNqa.HasValue)
            nqaIdDb = await _unitOfWork.PlanEchanRepository.GetOrCreateNqaAsync(dto.ValeurNqa.Value);
        return PlanEchanMapper.ConstruireNouveauPlan(dto, nqaIdDb, user);
    }

    protected override async Task ApplierMiseAJourDraftAsync(PlanEchantillonnageEntete plan, UpdatePlanEchanRequestDto dto, string user)
    {
        plan.NiveauControle = dto.NiveauControle;
        plan.TypePlan = dto.TypePlan;
        plan.ModeControle = dto.ModeControle;
        if (dto.ValeurNqa.HasValue)
            plan.NqaId = await _unitOfWork.PlanEchanRepository.GetOrCreateNqaAsync(dto.ValeurNqa.Value);
        else if (dto.NqaId.HasValue)
            plan.NqaId = dto.NqaId.Value;
        plan.Remarques = dto.Remarques;
        plan.LegendeMoyens = dto.LegendeMoyens;




        plan.ModifiePar = user;
        plan.ModifieLe = DateTime.UtcNow;
        if (dto.Regles != null)
        {
            plan.PlanEchantillonnageRegles.Clear();
            foreach (var r in dto.Regles)
                plan.PlanEchantillonnageRegles.Add(new PlanEchantillonnageRegle { Id = Guid.NewGuid(), FicheEnteteId = plan.Id, TailleMinLot = r.TailleMinLot, TailleMaxLot = r.TailleMaxLot, LettreCode = r.LettreCode, EffectifEchantillonA = r.EffectifEchantillonA, NbPostesB = r.NbPostesB, EffectifParPosteAb = r.EffectifParPosteAb, CritereAcceptationAc = r.CritereAcceptationAc, CritereRejetRe = r.CritereRejetRe });
        }
    }

    protected override async Task PersisterEntiteAsync(PlanEchantillonnageEntete plan) => await _unitOfWork.PlanEchanRepository.AddPlanAsync(plan);

    protected override async Task<int> CalculerNouvelleVersionAsync(PlanEchantillonnageEntete plan)
    {
        // Pour Echantillonnage, il y a typiquement un seul plan actif
        // On retourne simplement Version + 1
        return plan.Version + 1;
    }

    protected override async Task<PlanEchantillonnageEntete> CreerNouvelleVersionEntiteAsync(PlanEchantillonnageEntete ancienPlan, UpdatePlanEchanRequestDto dto, int nouvelleVersion, string user)
    {
        int nqaId = dto.NqaId ?? ancienPlan.NqaId;
        if (dto.ValeurNqa.HasValue)
            nqaId = await _unitOfWork.PlanEchanRepository.GetOrCreateNqaAsync(dto.ValeurNqa.Value);

        var nouveauPlan = new PlanEchantillonnageEntete { Id = Guid.NewGuid(), NiveauControle = dto.NiveauControle ?? ancienPlan.NiveauControle, TypePlan = dto.TypePlan ?? ancienPlan.TypePlan, ModeControle = dto.ModeControle ?? ancienPlan.ModeControle, NqaId = nqaId, Version = nouvelleVersion, Statut = StatutsPlan.Brouillon, CreePar = user, CreeLe = DateTime.UtcNow, Remarques = dto.Remarques ?? ancienPlan.Remarques, LegendeMoyens = dto.LegendeMoyens ?? ancienPlan.LegendeMoyens };

        // Copier les règles (soit nouvelles, soit anciennes)
        var regles = dto.Regles?.Any() == true ? dto.Regles : null;
        if (regles != null)
        {
            nouveauPlan.PlanEchantillonnageRegles = regles.Select(r => new PlanEchantillonnageRegle { Id = Guid.NewGuid(), FicheEnteteId = nouveauPlan.Id, TailleMinLot = r.TailleMinLot, TailleMaxLot = r.TailleMaxLot, LettreCode = r.LettreCode, EffectifEchantillonA = r.EffectifEchantillonA, NbPostesB = r.NbPostesB, EffectifParPosteAb = r.EffectifParPosteAb, CritereAcceptationAc = r.CritereAcceptationAc, CritereRejetRe = r.CritereRejetRe }).ToList();
        }
        else
        {
            nouveauPlan.PlanEchantillonnageRegles = ancienPlan.PlanEchantillonnageRegles.Select(r => new PlanEchantillonnageRegle { Id = Guid.NewGuid(), FicheEnteteId = nouveauPlan.Id, TailleMinLot = r.TailleMinLot, TailleMaxLot = r.TailleMaxLot, LettreCode = r.LettreCode, EffectifEchantillonA = r.EffectifEchantillonA, NbPostesB = r.NbPostesB, EffectifParPosteAb = r.EffectifParPosteAb, CritereAcceptationAc = r.CritereAcceptationAc, CritereRejetRe = r.CritereRejetRe }).ToList();
        }

        return nouveauPlan;
    }

    protected override async Task<PlanEchantillonnageEntete?> ObtenirBrouillonExistantAsync(CreatePlanEchanRequestDto dto)
    {
        // Pour Echantillonnage, unicité garantie: un seul plan à la fois
        return null;
    }

    public async Task<PlanEchanResponseDto> GetPlanByIdAsync(Guid planId)
    {
        var plan = await _unitOfWork.PlanEchanRepository.GetPlanAvecRelationsAsync(planId);
        if (plan == null) throw new Exception("Plan introuvable.");
        return PlanEchanMapper.MapperEntiteVersDto(plan);
    }

    public async Task<PlanEchanResponseDto?> GetPlanActifAsync()
    {
        var plan = await _unitOfWork.PlanEchanRepository.GetPlanActifAsync();
        return plan != null ? PlanEchanMapper.MapperEntiteVersDto(plan) : null;
    }

    public async Task<Guid> CreerPlanAsync(CreatePlanEchanRequestDto request, string creePar) => await CreerBrouillonAsync(request, creePar);

    public async Task<bool> MettreAJourPlanAsync(Guid planId, UpdatePlanEchanRequestDto request)
    {
        await UpdateDraftAsync(planId, request, request.ModifiePar ?? "");
        return true;
    }

    public async Task<Guid> CreerNouvelleVersionAsync(NouvelleVersionEchanRequestDto request)
    {
        var ancienPlan = await _unitOfWork.PlanEchanRepository.GetPlanAvecRelationsAsync(request.AncienId);
        if (ancienPlan == null) throw new Exception("Plan introuvable.");
        if (ancienPlan.Statut == StatutsPlan.Archive) throw new Exception("Impossible de versionner un plan archivé.");

        ancienPlan.Statut = StatutsPlan.Archive;
        ancienPlan.ModifiePar = request.ModifiePar;
        ancienPlan.ModifieLe = DateTime.UtcNow;

        var nouvelleVersion = await CalculerNouvelleVersionAsync(ancienPlan);
        var updateDto = request.Donnees ?? new UpdatePlanEchanRequestDto { NiveauControle = ancienPlan.NiveauControle, TypePlan = ancienPlan.TypePlan, ModeControle = ancienPlan.ModeControle, NqaId = ancienPlan.NqaId, ModifiePar = request.ModifiePar };

        var nouveauPlan = await CreerNouvelleVersionEntiteAsync(ancienPlan, updateDto, nouvelleVersion, request.ModifiePar);
        nouveauPlan.Statut = StatutsPlan.Actif;

        await PersisterEntiteAsync(nouveauPlan);
        await _unitOfWork.CommitAsync();
        return nouveauPlan.Id;
    }

    public async Task<Guid> RestaurerPlanAsync(RestaurerEchanRequestDto request)
    {
        var planArchive = await _unitOfWork.PlanEchanRepository.GetPlanAvecRelationsAsync(request.ArchiveId);
        if (planArchive == null || planArchive.Statut != StatutsPlan.Archive) throw new Exception("Plan archivé introuvable.");

        var nouveauPlanActif = PlanEchanMapper.DupliquerEntitePlan(planArchive, request.ModifiePar, request.MotifRestauration);
        nouveauPlanActif.Statut = StatutsPlan.Actif;

        var ancienPlanActif = await _unitOfWork.PlanEchanRepository.GetPlanActifAsync();
        if (ancienPlanActif?.Id != planArchive.Id)
        {
            ancienPlanActif.Statut = StatutsPlan.Archive;
            ancienPlanActif.ModifieLe = DateTime.UtcNow;
            ancienPlanActif.ModifiePar = request.ModifiePar;
        }

        await _unitOfWork.PlanEchanRepository.AddPlanAsync(nouveauPlanActif);
        await _unitOfWork.CommitAsync();
        return nouveauPlanActif.Id;
    }
}

