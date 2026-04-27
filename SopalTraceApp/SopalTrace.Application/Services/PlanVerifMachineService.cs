using FluentValidation;
using SopalTrace.Application.DTOs.QualityPlans.VerifMachine;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Mappers;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Application.Services;

public class PlanVerifMachineService : IPlanVerifMachineService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreatePlanVerifMachineDto> _createValidator;

    public PlanVerifMachineService(IUnitOfWork unitOfWork, IValidator<CreatePlanVerifMachineDto> createValidator)
    {
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
    }

    // =========================================================================
    // LECTURE
    // =========================================================================
    public async Task<PlanVerifMachineResponseDto> GetPlanVerifByIdAsync(Guid planId)
    {
        var plan = await _unitOfWork.PlanVerifMachineRepository.GetPlanAvecRelationsAsync(planId);
        if (plan == null) throw new Exception("Plan introuvable.");
        return PlanVerifMachineMapper.MapperEntiteVersDto(plan);
    }

    public async Task<List<PlanVerifMachineResponseDto>> GetTousLesPlansVerifAsync()
    {
        var plans = await _unitOfWork.PlanVerifMachineRepository.GetTousLesPlanAsync();
        return plans.Select(p => PlanVerifMachineMapper.MapperEntiteVersDto(p)).ToList();
    }

    // Rétrocompatibilité si vous l'utilisez ailleurs
    public async Task<PlanVerifMachineResponseDto> GetPlanByIdAsync(Guid planId)
    {
        return await GetPlanVerifByIdAsync(planId);
    }


    // =========================================================================
    // CRÉATION
    // =========================================================================
   public async Task<Guid> CreerPlanVerifAsync(CreateVerifMachineModeleDto request, string creePar)
    {
        var nouveauPlan = PlanVerifMachineMapper.ConstruireDepuisModeleDto(request, creePar);

        // 1. Récupérer uniquement le plan actif actuel pour l'archiver
        var planActif = await _unitOfWork.PlanVerifMachineRepository.GetPlanActifAsync(request.MachineCode);
        
        if (planActif != null)
        {
            planActif.Statut = "ARCHIVE";
            planActif.ModifieLe = DateTime.UtcNow;
            planActif.ModifiePar = creePar;
            
            // On sauvegarde l'archivage en premier pour éviter tout conflit d'index unique sur (MachineCode, Statut)
            await _unitOfWork.CommitAsync();
        }

        // 2. Déterminer la version V+1 (on charge tous les plans car on n'a pas de méthode MaxVersion)
        var tousLesPlans = await _unitOfWork.PlanVerifMachineRepository.GetTousLesPlanAsync();
        var maxVersionExistante = tousLesPlans
            .Where(p => p.MachineCode == request.MachineCode)
            .Max(p => p.Version) ?? 0;
        
        nouveauPlan.Version = maxVersionExistante + 1;
        nouveauPlan.Statut = "ACTIF";

        await _unitOfWork.PlanVerifMachineRepository.AddPlanAsync(nouveauPlan);
        await _unitOfWork.CommitAsync();
        
        return nouveauPlan.Id;
    }

    public async Task<Guid> CreerPlanAsync(CreatePlanVerifMachineDto request, string creePar)
    {
        var nouveauPlan = PlanVerifMachineMapper.ConstruireNouveauPlan(request, creePar);
        await _unitOfWork.PlanVerifMachineRepository.AddPlanAsync(nouveauPlan);
        await _unitOfWork.CommitAsync();
        return nouveauPlan.Id;
    }


    // =========================================================================
    // ÉDITION ET VERSIONING (LA NOUVELLE LOGIQUE !)
    // =========================================================================
    
    // Remarque : Changement de la signature pour retourner un Task<Guid> (le nouvel ID V+1)
    public async Task<Guid> MettreAJourPlanVerifAsync(Guid planIdActuel, CreateVerifMachineModeleDto request, string modifiePar)
    {
        // 1. On récupère l'ancien plan. 
        // Note : Si vous avez une méthode GetByIdAsync (sans les relations), utilisez-la ! 
        // Sinon GetPlanAvecRelationsAsync fonctionne, mais on va tout faire en 1 seul Commit.
        var planActuel = await _unitOfWork.PlanVerifMachineRepository.GetPlanAvecRelationsAsync(planIdActuel);
        if (planActuel == null) throw new Exception("Plan introuvable.");

        // 🛡️ SÉCURITÉ : Si l'utilisateur clique sur "Sauvegarder" sur un plan DÉJÀ archivé
        if (planActuel.Statut == "ARCHIVE")
        {
            throw new Exception("Ce plan a déjà été archivé. Veuillez recharger la page pour voir la dernière version active.");
        }

        // 2. Archiver l'ancien plan
        planActuel.Statut = "ARCHIVE";
        planActuel.ModifiePar = modifiePar;
        planActuel.ModifieLe = DateTime.UtcNow;

        // 3. Créer le NOUVEAU plan basé sur les modifs du frontend
        var nouveauPlan = PlanVerifMachineMapper.ConstruireDepuisModeleDto(request, modifiePar);
        
        // 4. Lier l'historique et sécuriser la version
        nouveauPlan.MachineCode = planActuel.MachineCode; 
        
        // Sécurité maximale : On cherche la version la plus haute existante dans la base
        var tousLesPlans = await _unitOfWork.PlanVerifMachineRepository.GetTousLesPlanAsync();
        var maxVersion = tousLesPlans
            .Where(p => p.MachineCode == planActuel.MachineCode)
            .Max(p => p.Version) ?? 0;
            
        nouveauPlan.Version = maxVersion + 1; // Incrémentation propre (V+1)

        // 5. On prépare l'insertion du nouveau plan
        await _unitOfWork.PlanVerifMachineRepository.AddPlanAsync(nouveauPlan);
        
        // 🚀 LA MAGIE EST ICI : UN SEUL COMMIT !
        // EF Core va automatiquement faire l'UPDATE de l'ancien et l'INSERT du nouveau en une seule transaction SQL sécurisée.
        await _unitOfWork.CommitAsync();

        return nouveauPlan.Id;
    }
    public async Task<Guid> CreerNouvelleVersionAsync(NouvelleVersionVerifMachineDto request)
    {
        var ancienPlan = await _unitOfWork.PlanVerifMachineRepository.GetPlanAvecRelationsAsync(request.AncienId);
        if (ancienPlan == null) throw new Exception("Plan introuvable.");

        // 1. Archiver l'ancien
        ancienPlan.Statut = "ARCHIVE";
        ancienPlan.ModifieLe = DateTime.UtcNow;
        ancienPlan.ModifiePar = request.ModifiePar;

        // 2. Dupliquer via le Mapper
        var nouveauPlan = PlanVerifMachineMapper.DupliquerEntitePlan(ancienPlan, request.ModifiePar, request.MotifModification);

        // 3. 🛡️ SÉCURITÉ : Garantir que la nouvelle version est vraiment la Max + 1
        var tousLesPlans = await _unitOfWork.PlanVerifMachineRepository.GetTousLesPlanAsync();
        var maxVersion = tousLesPlans
            .Where(p => p.MachineCode == ancienPlan.MachineCode)
            .Max(p => p.Version) ?? 0;
            
        nouveauPlan.Version = maxVersion + 1;

        // 4. Sauvegarder
        await _unitOfWork.PlanVerifMachineRepository.AddPlanAsync(nouveauPlan);
        await _unitOfWork.CommitAsync();

        return nouveauPlan.Id;
    }

    // =========================================================================
    // CHANGEMENTS DE STATUT
    // =========================================================================
    public async Task ArchiverPlanAsync(Guid planId, string modifiePar)
    {
        var plan = await _unitOfWork.PlanVerifMachineRepository.GetPlanAvecRelationsAsync(planId);
        if (plan == null) throw new Exception("Plan introuvable.");

        plan.Statut = "ARCHIVE";
        plan.ModifiePar = modifiePar;
        plan.ModifieLe = DateTime.UtcNow;

        await _unitOfWork.CommitAsync();
    }

    public async Task<Guid> RestaurerPlanAsync(Guid planId, string restaurePar, string motif)
    {
        var planArchived = await _unitOfWork.PlanVerifMachineRepository.GetPlanAvecRelationsAsync(planId);
        if (planArchived == null) throw new Exception("Plan archivé introuvable.");

        // 1. Archiver le plan ACTIF actuel pour cette machine
        var planActuel = await _unitOfWork.PlanVerifMachineRepository.GetPlanActifAsync(planArchived.MachineCode);
        if (planActuel != null)
        {
            planActuel.Statut = StatutsPlan.Archive;
            planActuel.ModifiePar = restaurePar;
            planActuel.ModifieLe = DateTime.UtcNow;
            // On ne commit pas tout de suite pour l'atomicité
        }

        // 2. Calculer la version max globale pour cette machine
        var tousLesPlans = await _unitOfWork.PlanVerifMachineRepository.GetTousLesPlanAsync();
        var maxVersion = tousLesPlans
            .Where(p => p.MachineCode == planArchived.MachineCode)
            .Max(p => p.Version) ?? 0;

        // 3. Créer une nouvelle version basée sur l'archive
        var nouveauPlan = PlanVerifMachineMapper.DupliquerEntitePlan(planArchived, restaurePar, motif);
        nouveauPlan.Statut = StatutsPlan.Actif;
        nouveauPlan.Version = maxVersion + 1;
        nouveauPlan.CreeLe = DateTime.UtcNow;
        nouveauPlan.CreePar = restaurePar;

        await _unitOfWork.PlanVerifMachineRepository.AddPlanAsync(nouveauPlan);
        await _unitOfWork.CommitAsync();

        return nouveauPlan.Id;
    }

    // =========================================================================
    // ANCIENNE MÉTHODE (À garder si vous éditez des brouillons)
    // =========================================================================
    public async Task<bool> MettreAJourValeursPlanAsync(Guid planId, List<VerifMachineLigneEditDto> lignesModifiees)
    {
        var plan = await _unitOfWork.PlanVerifMachineRepository.GetPlanAvecRelationsAsync(planId);
        if (plan == null) return false;

        // Protection ISO 9001 : Si le plan est actif, on ne doit pas le modifier directement, on doit versionner.
        if (plan.Statut == "ACTIF") 
            throw new Exception("Interdit: Un plan ACTIF ne peut pas être modifié directement. Veuillez créer une nouvelle version.");

        foreach (var lDto in lignesModifiees)
        {
            var isNewLigne = !lDto.Id.HasValue || lDto.Id.Value == Guid.Empty;
            var ligneEnBase = isNewLigne ? null : plan.PlanVerifMachineLignes.FirstOrDefault(l => l.Id == lDto.Id.Value);

            if (ligneEnBase == null)
            {
                var nouvelleLigne = new PlanVerifMachineLigne
                {
                    PlanEnteteId = planId,
                    OrdreAffiche = lDto.OrdreAffiche, 
                    LibelleRisque = lDto.LibelleRisque,
                    LibelleMethode = lDto.LibelleMethode,
                    TypeLigne = string.IsNullOrEmpty(lDto.TypeLigne) ? "RISQUE" : lDto.TypeLigne, 
                    
                    PlanVerifMachineEcheances = lDto.Echeances
                        .Where(e => e.PeriodiciteId != Guid.Empty) 
                        .Select(eDto => new PlanVerifMachineEcheance
                        {
                            PeriodiciteId = eDto.PeriodiciteId,
                            OrdreAffiche = eDto.OrdreAffiche, 
                            RefMoyenDetectionId = eDto.RefMoyenDetectionId, 
                            
                            PlanVerifMachineMatricePieces = eDto.PiecesRef.Select(pDto => new PlanVerifMachineMatricePiece
                            {
                                FamilleId = pDto.FamilleId, 
                                PieceRefId = (pDto.PieceRefId.HasValue && pDto.PieceRefId.Value != Guid.Empty) ? pDto.PieceRefId : null, 
                                RoleVerif = pDto.RoleVerif
                            }).ToList()
                        }).ToList()
                };
                plan.PlanVerifMachineLignes.Add(nouvelleLigne);
            }
        }

        await _unitOfWork.CommitAsync();
        return true;
    }
}