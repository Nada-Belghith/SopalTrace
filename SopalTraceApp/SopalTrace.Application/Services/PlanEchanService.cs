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

public class PlanEchanService : IPlanEchanService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreatePlanEchanRequestDto> _createValidator;
    private readonly ILogger<PlanEchanService> _logger;

    public PlanEchanService(IUnitOfWork unitOfWork, IValidator<CreatePlanEchanRequestDto> createValidator, ILogger<PlanEchanService> logger)
    {
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _logger = logger;
    }

    public async Task<Guid> CreerPlanAsync(CreatePlanEchanRequestDto request, string creePar)
    {
        var valResult = await _createValidator.ValidateAsync(request);
        if (!valResult.IsValid) throw new ValidationException(valResult.Errors);

        if (await _unitOfWork.PlanEchanRepository.ExistePlanActifAsync())
            throw new Exception("Un profil d'échantillonnage ACTIF existe déjà.");

        // Gérer correctement la table NQA V6.1
        int nqaIdDb = request.NqaId ?? 0;
        if (request.ValeurNqa.HasValue)
        {
            nqaIdDb = await _unitOfWork.PlanEchanRepository.GetOrCreateNqaAsync(request.ValeurNqa.Value);
        }

        var nouveauPlan = PlanEchanMapper.ConstruireNouveauPlan(request, nqaIdDb, creePar);

        await _unitOfWork.PlanEchanRepository.AddPlanAsync(nouveauPlan);
        await _unitOfWork.CommitAsync();

        return nouveauPlan.Id;
    }

    public async Task<PlanEchanResponseDto> GetPlanByIdAsync(Guid planId)
    {
        var plan = await _unitOfWork.PlanEchanRepository.GetPlanAvecRelationsAsync(planId);
        if (plan == null) throw new Exception("Plan introuvable.");

        return PlanEchanMapper.MapperEntiteVersDto(plan);
    }

    public async Task<bool> MettreAJourPlanAsync(Guid planId, UpdatePlanEchanRequestDto request)
    {
        var plan = await _unitOfWork.PlanEchanRepository.GetPlanAvecRelationsAsync(planId);
        if (plan == null) return false;

        // Mise à jour des valeurs du plan
        plan.NiveauControle = request.NiveauControle;
        plan.TypePlan = request.TypePlan;
        plan.ModeControle = request.ModeControle;
        
        if (request.ValeurNqa.HasValue)
        {
            plan.NqaId = await _unitOfWork.PlanEchanRepository.GetOrCreateNqaAsync(request.ValeurNqa.Value);
        }
        else if (request.NqaId.HasValue)
        {
            plan.NqaId = request.NqaId.Value;
        }

        plan.Remarques = request.Remarques;
        plan.LegendeMoyens = request.LegendeMoyens;
        plan.ModifiePar = request.ModifiePar;
        plan.ModifieLe = DateTime.UtcNow;

        // Mise à jour des règles
        if (request.Regles != null)
        {
            plan.PlanEchantillonnageRegles.Clear();
            foreach (var r in request.Regles)
            {
                plan.PlanEchantillonnageRegles.Add(new PlanEchantillonnageRegle
                {
                    Id = Guid.NewGuid(),
                    FicheEnteteId = plan.Id,
                    TailleMinLot = r.TailleMinLot,
                    TailleMaxLot = r.TailleMaxLot,
                    LettreCode = r.LettreCode,
                    EffectifEchantillonA = r.EffectifEchantillonA,
                    NbPostesB = r.NbPostesB,
                    EffectifParPosteAb = r.EffectifParPosteAb,
                    CritereAcceptationAc = r.CritereAcceptationAc,
                    CritereRejetRe = r.CritereRejetRe
                });
            }
        }

        // Fin de mise à jour direct
        await _unitOfWork.CommitAsync();
        return true;
    }

    public async Task<Guid> CreerNouvelleVersionAsync(NouvelleVersionEchanRequestDto request)
    {
        var ancienPlan = await _unitOfWork.PlanEchanRepository.GetPlanAvecRelationsAsync(request.AncienId);
        if (ancienPlan == null) throw new Exception("Plan introuvable.");
        if (ancienPlan.Statut == StatutsPlan.Archive) throw new Exception("Impossible de versionner un plan archivé.");

        // ✅ ARCHIVAGE : On archive l'ancien plan pour libérer la place
        ancienPlan.Statut = StatutsPlan.Archive;
        ancienPlan.ModifiePar = request.ModifiePar;
        ancienPlan.ModifieLe = DateTime.UtcNow;
        ancienPlan.CommentaireVersion = $"Archivé pour création de la V{ancienPlan.Version + 1}";
        
        // ✅ CRÉATION : On crée la nouvelle version avec les données modifiées du UI
        int nqaId = request.Donnees?.NqaId ?? ancienPlan.NqaId;
        if (request.Donnees?.ValeurNqa.HasValue == true)
        {
            nqaId = await _unitOfWork.PlanEchanRepository.GetOrCreateNqaAsync(request.Donnees.ValeurNqa.Value);
        }

        var nouveauPlan = new PlanEchantillonnageEntete
        {
            Id = Guid.NewGuid(),
            NiveauControle = request.Donnees?.NiveauControle ?? ancienPlan.NiveauControle,
            TypePlan = request.Donnees?.TypePlan ?? ancienPlan.TypePlan,
            ModeControle = request.Donnees?.ModeControle ?? ancienPlan.ModeControle,
            NqaId = nqaId,
            Version = ancienPlan.Version + 1,
            Statut = StatutsPlan.Actif, // On l'active direct !
            CreePar = request.ModifiePar,
            CreeLe = DateTime.UtcNow,
            CommentaireVersion = request.MotifModification,
            Remarques = request.Donnees?.Remarques ?? ancienPlan.Remarques,
            LegendeMoyens = request.Donnees?.LegendeMoyens ?? ancienPlan.LegendeMoyens
        };

        // Transfert des règles modifiées
        if (request.Donnees?.Regles != null && request.Donnees.Regles.Any())
        {
            nouveauPlan.PlanEchantillonnageRegles = request.Donnees.Regles.Select(r => new PlanEchantillonnageRegle
            {
                Id = Guid.NewGuid(),
                FicheEnteteId = nouveauPlan.Id,
                TailleMinLot = r.TailleMinLot,
                TailleMaxLot = r.TailleMaxLot,
                LettreCode = r.LettreCode,
                EffectifEchantillonA = r.EffectifEchantillonA,
                NbPostesB = r.NbPostesB,
                EffectifParPosteAb = r.EffectifParPosteAb,
                CritereAcceptationAc = r.CritereAcceptationAc,
                CritereRejetRe = r.CritereRejetRe
            }).ToList();
        }
        else
        {
            // Fallback sur les anciennes règles
            nouveauPlan.PlanEchantillonnageRegles = ancienPlan.PlanEchantillonnageRegles.Select(r => new PlanEchantillonnageRegle
            {
                Id = Guid.NewGuid(),
                FicheEnteteId = nouveauPlan.Id,
                TailleMinLot = r.TailleMinLot,
                TailleMaxLot = r.TailleMaxLot,
                LettreCode = r.LettreCode,
                EffectifEchantillonA = r.EffectifEchantillonA,
                NbPostesB = r.NbPostesB,
                EffectifParPosteAb = r.EffectifParPosteAb,
                CritereAcceptationAc = r.CritereAcceptationAc,
                CritereRejetRe = r.CritereRejetRe
            }).ToList();
        }

        await _unitOfWork.PlanEchanRepository.AddPlanAsync(nouveauPlan);
        await _unitOfWork.CommitAsync();

        return nouveauPlan.Id;
    }

    public async Task<Guid> RestaurerPlanAsync(RestaurerEchanRequestDto request)
    {
        // 1. Récupérer le plan archivé complet avec ses relations (règles)
        var planArchive = await _unitOfWork.PlanEchanRepository.GetPlanAvecRelationsAsync(request.ArchiveId);
        
        if (planArchive == null) 
            throw new Exception("Le plan archivé à restaurer est introuvable.");

        // ISO 9001 : On s'assure que c'est bien une version archivée
        if (planArchive.Statut != StatutsPlan.Archive)
            throw new Exception($"Impossible de restaurer ce plan car il n'est pas archivé (Statut actuel : {planArchive.Statut}).");

        // 2. Dupliquer le plan archivé pour créer une nouvelle version
        // Le mapper gère l'ID, la version +1, le statut Brouillon initial, etc.
        var nouveauPlanActif = PlanEchanMapper.DupliquerEntitePlan(planArchive, request.ModifiePar, request.MotifRestauration);
        
        // 3. Forcer le statut à ACTIF pour ce nouveau plan
        nouveauPlanActif.Statut = StatutsPlan.Actif;
        // La date de création et le créateur du nouveau plan sont gérés par le mapper

        // 4. Gérer l'unicité du plan ACTIF (ISO 9001)
        // On cherche s'il existe un plan actuellement ACTIF (Global)
        var ancienPlanActif = await _unitOfWork.PlanEchanRepository.GetPlanActifAsync();
        
        // S'il existe, on l'archive pour laisser la place au plan restauré
        if (ancienPlanActif != null)
        {
            ancienPlanActif.Statut = StatutsPlan.Archive;
            ancienPlanActif.ModifieLe = DateTime.UtcNow;
            ancienPlanActif.ModifiePar = request.ModifiePar;
            ancienPlanActif.CommentaireVersion = $"Archivé automatiquement suite à la restauration de la version {planArchive.Version}.";
        }

        // 5. Sauvegarder les modifications dans la base de données
        await _unitOfWork.PlanEchanRepository.AddPlanAsync(nouveauPlanActif);
        await _unitOfWork.CommitAsync();

        // Retourner l'ID du nouveau plan ACTIF
        return nouveauPlanActif.Id;
    }
}