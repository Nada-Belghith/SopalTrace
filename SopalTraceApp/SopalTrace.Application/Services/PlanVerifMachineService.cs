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
    private readonly ICatalogueReferentielService _referentielService;
    private readonly IFormulairePrcService _formulaireService;

    public PlanVerifMachineService(IUnitOfWork unitOfWork, IValidator<CreatePlanVerifMachineDto> createValidator, ICatalogueReferentielService referentielService, IFormulairePrcService formulaireService)
    {
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _referentielService = referentielService;
        _formulaireService = formulaireService;
    }

    public virtual string Role => "DEFAULT";

    // =========================================================================
    // LECTURE
    // =========================================================================
    public virtual async Task<PlanVerifMachineResponseDto> GetPlanVerifByIdAsync(Guid planId)
    {
        var plan = await _unitOfWork.PlanVerifMachineRepository.GetPlanAvecRelationsAsync(planId);
        if (plan == null) throw new Exception("Plan introuvable.");
        return PlanVerifMachineMapper.MapperEntiteVersDto(plan);
    }

    public virtual async Task<List<PlanVerifMachineResponseDto>> GetTousLesPlansVerifAsync()
    {
        var plans = await _unitOfWork.PlanVerifMachineRepository.GetTousLesPlanAsync();
        return plans.Select(p => PlanVerifMachineMapper.MapperEntiteVersDto(p)).ToList();
    }

    /// <summary>
    /// Retourne les familles de corps configurées pour une machine dans Machine_FamilleCorps.
    /// </summary>
    public virtual async Task<List<FamilleCorpsDto>> GetFamillesParMachineAsync(string machineCode)
    {
        var familles = await _unitOfWork.PlanVerifMachineRepository.GetFamillesParMachineAsync(machineCode);
        return familles.Select(f => new FamilleCorpsDto
        {
            Id = f.Id,
            Code = f.Code,
            Designation = f.Designation
        }).ToList();
    }

    // Rétrocompatibilité si vous l'utilisez ailleurs
    public async Task<PlanVerifMachineResponseDto> GetPlanByIdAsync(Guid planId)
    {
        return await GetPlanVerifByIdAsync(planId);
    }


    // =========================================================================
    // CRÉATION
    // =========================================================================
   public virtual async Task<Guid> CreerPlanVerifAsync(CreateVerifMachineModeleDto request, string creePar)
    {
        var nouveauPlan = PlanVerifMachineMapper.ConstruireDepuisModeleDto(request, creePar);

        // 1. Récupérer uniquement le plan actif lié au MÊME formulaire pour l'archiver
        // (FE-VM-MAS20 != FE-VM-MAS22 = plans indépendants même si même machine physique)
        PlanVerifMachineEntete? planActif = null;
        // Chercher le formulaire actif pour ce code machine spécifique
        var codeRefMachine = $"FE-VM-{request.MachineCode}";
        var formActif = await _formulaireService.GetFormulaireActifParCodeReferenceAsync(codeRefMachine);
        if (formActif != null)
        {
            planActif = await _unitOfWork.PlanVerifMachineRepository.GetPlanActifParFormulaireAsync(formActif.Id);
        }
        if (planActif == null)
        {
            // Fallback par MachineCode
            planActif = await _unitOfWork.PlanVerifMachineRepository.GetPlanActifAsync(request.MachineCode);
        }
        
        if (planActif != null)
        {
            planActif.Statut = StatutsPlan.Archive;
            //planActif.ModifieLe = DateTime.UtcNow;
            //planActif.ModifiePar = creePar;
            
            // On sauvegarde l'archivage en premier pour éviter tout conflit d'index unique sur (MachineCode, Statut)
            await _unitOfWork.CommitAsync();
        }

        // 2. Déterminer la version V+1 (on charge tous les plans car on n'a pas de méthode MaxVersion)
        var tousLesPlans = await _unitOfWork.PlanVerifMachineRepository.GetTousLesPlanAsync();
        var maxVersionExistante = tousLesPlans
            .Where(p => p.MachineCode == request.MachineCode)
            .Max(p => (int?)p.Version);
        
        nouveauPlan.Version = maxVersionExistante.HasValue ? maxVersionExistante.Value + 1 : (request.VersionInitiale ?? 0);
        nouveauPlan.Statut = StatutsPlan.Actif;

        if (!string.IsNullOrEmpty(request.ConfigurationColonnesJson))
        {
            var formResult = await _formulaireService.UpdateFormulaireStructureAsync("VERIF_MACHINE", request.ConfigurationColonnesJson, $"FE-VM-{request.MachineCode}", request.VersionInitiale);
            if (formResult.HasValue)
            {
                nouveauPlan.FormulaireId = formResult.Value.Id;
                nouveauPlan.Version = formResult.Value.Version;
            }
        }

        await _unitOfWork.PlanVerifMachineRepository.AddPlanAsync(nouveauPlan);
        
        if (request.Familles != null)
        {
            await _unitOfWork.PlanVerifMachineRepository.SyncMachineFamillesAsync(
                request.MachineCode, 
                request.Familles.Select(f => f.RefFamilleCorpsId).ToList()
            );
        }

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
    public virtual async Task<Guid> MettreAJourPlanVerifAsync(Guid planIdActuel, CreateVerifMachineModeleDto request, string modifiePar)
    {
        // 1. On récupère l'ancien plan. 
        // Note : Si vous avez une méthode GetByIdAsync (sans les relations), utilisez-la ! 
        // Sinon GetPlanAvecRelationsAsync fonctionne, mais on va tout faire en 1 seul Commit.
        var planActuel = await _unitOfWork.PlanVerifMachineRepository.GetPlanAvecRelationsAsync(planIdActuel);
        if (planActuel == null) throw new Exception("Plan introuvable.");

        // 🛡️ SÉCURITÉ : Si l'utilisateur clique sur "Sauvegarder" sur un plan DÉJÀ archivé
        if (planActuel.Statut == StatutsPlan.Archive)
        {
            throw new Exception("Ce plan a déjà été archivé. Veuillez recharger la page pour voir la dernière version active.");
        }

        // 2. Archiver l'ancien plan
        planActuel.Statut = StatutsPlan.Archive;
        //planActuel.ModifiePar = modifiePar;
        //planActuel.ModifieLe = DateTime.UtcNow;

        // 3. Créer le NOUVEAU plan basé sur les modifs du frontend
        var nouveauPlan = PlanVerifMachineMapper.ConstruireDepuisModeleDto(request, modifiePar);
        
        // 4. Lier l'historique et sécuriser la version
        nouveauPlan.MachineCode = planActuel.MachineCode; 
        
        var tousLesPlans = await _unitOfWork.PlanVerifMachineRepository.GetTousLesPlanAsync();
        var maxVersion = tousLesPlans
            .Where(p => p.MachineCode == planActuel.MachineCode)
            .Max(p => (int?)p.Version) ?? 0;
            
        nouveauPlan.Version = maxVersion + 1; // Incrémentation propre (V+1)

        var jsonToUse = request.ConfigurationColonnesJson;
        if (string.IsNullOrEmpty(jsonToUse) && planActuel.FormulaireId.HasValue)
        {
            var oldForm = await _formulaireService.GetFormulaireByIdAsync(planActuel.FormulaireId.Value);
            if (oldForm != null) jsonToUse = oldForm.ConfigurationStructureJson;
        }

        if (!string.IsNullOrEmpty(jsonToUse) || planActuel.FormulaireId.HasValue)
        {
            var formResult = await _formulaireService.UpdateFormulaireStructureAsync("VERIF_MACHINE", jsonToUse, $"FE-VM-{planActuel.MachineCode}", request.VersionInitiale);
            if (formResult.HasValue)
            {
                nouveauPlan.FormulaireId = formResult.Value.Id;
                nouveauPlan.Version = formResult.Value.Version;
            }
        }

        // 5. On prépare l'insertion du nouveau plan
        await _unitOfWork.PlanVerifMachineRepository.AddPlanAsync(nouveauPlan);
        
        if (request.Familles != null)
        {
            await _unitOfWork.PlanVerifMachineRepository.SyncMachineFamillesAsync(
                nouveauPlan.MachineCode, 
                request.Familles.Select(f => f.RefFamilleCorpsId).ToList()
            );
        }

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
        ancienPlan.Statut = StatutsPlan.Archive;
        //ancienPlan.ModifieLe = DateTime.UtcNow;
        //ancien//////plan.ModifiePar = request.ModifiePar;

        // 2. Dupliquer via le Mapper
        var nouveauPlan = PlanVerifMachineMapper.DupliquerEntitePlan(ancienPlan, request.ModifiePar, request.MotifModification);

        var tousLesPlans = await _unitOfWork.PlanVerifMachineRepository.GetTousLesPlanAsync();
        var maxVersion = tousLesPlans
            .Where(p => p.MachineCode == ancienPlan.MachineCode)
            .Max(p => (int?)p.Version) ?? 0;
            
        nouveauPlan.Version = maxVersion + 1;

        var jsonToUse = request.ConfigurationColonnesJson;
        if (string.IsNullOrEmpty(jsonToUse) && ancienPlan.FormulaireId.HasValue)
        {
            var oldForm = await _formulaireService.GetFormulaireByIdAsync(ancienPlan.FormulaireId.Value);
            if (oldForm != null) jsonToUse = oldForm.ConfigurationStructureJson;
        }

        if (!string.IsNullOrEmpty(jsonToUse) || ancienPlan.FormulaireId.HasValue)
        {
            var formResult = await _formulaireService.UpdateFormulaireStructureAsync("VERIF_MACHINE", jsonToUse, $"FE-VM-{ancienPlan.MachineCode}");
            if (formResult.HasValue)
            {
                nouveauPlan.FormulaireId = formResult.Value.Id;
                nouveauPlan.Version = formResult.Value.Version;
            }
        }

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

        plan.Statut = StatutsPlan.Archive;
        ////////plan.ModifiePar = modifiePar;
        ////plan.ModifieLe = DateTime.UtcNow;

        await _unitOfWork.CommitAsync();
    }

    public async Task<Guid> RestaurerPlanAsync(Guid planId, string restaurePar, string motif)
    {
        var planArchived = await _unitOfWork.PlanVerifMachineRepository.GetPlanAvecRelationsAsync(planId);
        if (planArchived == null) throw new Exception("Plan archivé introuvable.");

        // 1. Archiver le plan ACTIF lié au même formulaire (pas à la même machine générique)
        PlanVerifMachineEntete? planActuel = null;
        if (planArchived.FormulaireId.HasValue)
        {
            planActuel = await _unitOfWork.PlanVerifMachineRepository.GetPlanActifParFormulaireAsync(planArchived.FormulaireId.Value);
        }
        if (planActuel == null)
        {
            // Fallback par MachineCode
            planActuel = await _unitOfWork.PlanVerifMachineRepository.GetPlanActifAsync(planArchived.MachineCode);
        }
        if (planActuel != null)
        {
            planActuel.Statut = StatutsPlan.Archive;
            ////planActuel.ModifiePar = restaurePar;
            //planActuel.ModifieLe = DateTime.UtcNow;
            // On ne commit pas tout de suite pour l'atomicité
        }

        // 2. Calculer la version max globale pour cette machine
        var tousLesPlans = await _unitOfWork.PlanVerifMachineRepository.GetTousLesPlanAsync();
        var maxVersion = tousLesPlans
            .Where(p => p.MachineCode == planArchived.MachineCode)
            .Max(p => (int?)p.Version) ?? 0;

        Guid? newFormulaireId = null;
        var nouvelleVersion = maxVersion + 1;

        if (planArchived.FormulaireId.HasValue)
        {
            var oldForm = await _formulaireService.GetFormulaireByIdAsync(planArchived.FormulaireId.Value);
            if (oldForm != null)
            {
                var formResult = await _formulaireService.UpdateFormulaireStructureAsync(
                    "VERIF_MACHINE",
                    oldForm.ConfigurationStructureJson,
                    oldForm.CodeReference);
                
                if (formResult.HasValue)
                {
                    newFormulaireId = formResult.Value.Id;
                    nouvelleVersion = formResult.Value.Version;
                }
            }
        }

        // 3. Créer une nouvelle version basée sur l'archive
        var nouveauPlan = PlanVerifMachineMapper.DupliquerEntitePlan(planArchived, restaurePar, motif);
        nouveauPlan.Statut = StatutsPlan.Actif;
        nouveauPlan.Version = nouvelleVersion;
        nouveauPlan.CreeLe = DateTime.UtcNow;
        nouveauPlan.CreePar = restaurePar;
        if (newFormulaireId.HasValue) nouveauPlan.FormulaireId = newFormulaireId.Value;

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
        if (plan.Statut == StatutsPlan.Actif) 
            throw new Exception("Interdit: Un plan ACTIF ne peut pas être modifié directement. Veuillez créer une nouvelle version.");

        foreach (var lDto in lignesModifiees)
        {
            var isNewLigne = !lDto.Id.HasValue || lDto.Id.Value == Guid.Empty;
            var ligneEnBase = isNewLigne ? null : plan.PlanVerifMachineLignes.FirstOrDefault(l => l.Id == lDto.Id!.Value);

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

