using FluentValidation;
using Microsoft.Extensions.Logging;
using SopalTrace.Application.DTOs.QualityPlans.VerifMachine;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Mappers;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities; // Nécessaire pour instancier les entités
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

    public async Task<Guid> CreerPlanAsync(CreatePlanVerifMachineDto request, string creePar)
    {
        var valResult = await _createValidator.ValidateAsync(request);
        if (!valResult.IsValid) throw new ValidationException(valResult.Errors);

        if (await _unitOfWork.PlanVerifMachineRepository.ExistePlanActifAsync(request.MachineCode, request.TypeRapport, request.TypeRobinetCode))
            throw new Exception("Un plan ACTIF existe déjà pour cette machine.");

        var nouveauPlan = PlanVerifMachineMapper.ConstruireNouveauPlan(request, creePar);

        await _unitOfWork.PlanVerifMachineRepository.AddPlanAsync(nouveauPlan);
        await _unitOfWork.CommitAsync();

        return nouveauPlan.Id;
    }

    public async Task<PlanVerifMachineResponseDto> GetPlanByIdAsync(Guid planId)
    {
        var plan = await _unitOfWork.PlanVerifMachineRepository.GetPlanAvecRelationsAsync(planId);
        if (plan == null) throw new Exception("Plan introuvable.");

        return PlanVerifMachineMapper.MapperEntiteVersDto(plan);
    }

    // =======================================================================
    // LOGIQUE DE MISE A JOUR EN CASCADE (4 Niveaux)
    // =======================================================================
    public async Task<bool> MettreAJourValeursPlanAsync(Guid planId, List<VerifMachineLigneEditDto> lignesModifiees)
    {
        var plan = await _unitOfWork.PlanVerifMachineRepository.GetPlanAvecRelationsAsync(planId);
        if (plan == null) return false;

        // 1. SUPPRESSION DES LIGNES ENFANTS (Non présentes dans la requête)
        var dtoLigneIds = lignesModifiees.Where(l => l.Id.HasValue).Select(l => l.Id.Value).ToList();
        var lignesToRemove = plan.PlanVerifMachineLignes.Where(l => !dtoLigneIds.Contains(l.Id)).ToList();

        // CORRECTION 1 : Utilisation du Remove de la collection pour un tracking parfait par EF Core
        foreach (var l in lignesToRemove) plan.PlanVerifMachineLignes.Remove(l);

        // 2. PARCOURS DES LIGNES (Niveau 2)
        foreach (var lDto in lignesModifiees)
        {
            var isNewLigne = !lDto.Id.HasValue || lDto.Id.Value == Guid.Empty;
            var ligneEnBase = isNewLigne ? null : plan.PlanVerifMachineLignes.FirstOrDefault(l => l.Id == lDto.Id.Value);

            if (ligneEnBase == null)
            {
                // CORRECTION 2 : Instanciation directe SANS ID forcé pour garantir un INSERT propre
                var nouvelleLigne = new PlanVerifMachineLigne
                {
                    PlanEnteteId = planId,
                    OrdreAffiche = lDto.OrdreAffiche,
                    LibelleRisque = lDto.LibelleRisque,
                    RisqueDefautId = lDto.RisqueDefautId,
                    LibelleMethode = lDto.LibelleMethode,
                    TypeSaisie = lDto.TypeSaisie,
                    ValeurNominale = lDto.ValeurNominale,
                    ToleranceSup = lDto.ToleranceSup,
                    ToleranceInf = lDto.ToleranceInf,
                    Unite = lDto.Unite,
                    Instruction = lDto.Instruction,
                    EstCritique = lDto.EstCritique,

                    PlanVerifMachineEcheances = lDto.Echeances.Select(eDto => new PlanVerifMachineEcheance
                    {
                        PeriodiciteId = eDto.PeriodiciteId,
                        OrdreAffiche = eDto.OrdreAffiche,
                        LibelleMoyen = eDto.LibelleMoyen,

                        PlanVerifMachinePieceRefs = eDto.PiecesRef.Select(pDto => new PlanVerifMachinePieceRef
                        {
                            PieceRefId = pDto.PieceRefId,
                            RoleVerif = pDto.RoleVerif,
                            ResultatAttendu = pDto.ResultatAttendu,
                            FamilleDesc = pDto.FamilleDesc,
                            Notes = pDto.Notes
                        }).ToList()
                    }).ToList()
                };
                plan.PlanVerifMachineLignes.Add(nouvelleLigne);
            }
            else
            {
                PlanVerifMachineMapper.MettreAJourLigne(ligneEnBase, lDto);

                // --- SUPPRESSION DES ECHEANCES (Niveau 3) ---
                var dtoEcheanceIds = lDto.Echeances.Where(e => e.Id.HasValue).Select(e => e.Id.Value).ToList();
                var echeancesToRemove = ligneEnBase.PlanVerifMachineEcheances.Where(e => !dtoEcheanceIds.Contains(e.Id)).ToList();
                foreach (var e in echeancesToRemove) ligneEnBase.PlanVerifMachineEcheances.Remove(e);

                // --- PARCOURS DES ECHEANCES ---
                foreach (var eDto in lDto.Echeances)
                {
                    var isNewEcheance = !eDto.Id.HasValue || eDto.Id.Value == Guid.Empty;
                    var echeanceEnBase = isNewEcheance ? null : ligneEnBase.PlanVerifMachineEcheances.FirstOrDefault(e => e.Id == eDto.Id.Value);

                    if (echeanceEnBase == null)
                    {
                        var nouvelleEcheance = new PlanVerifMachineEcheance
                        {
                            PlanLigneId = ligneEnBase.Id,
                            PeriodiciteId = eDto.PeriodiciteId,
                            OrdreAffiche = eDto.OrdreAffiche,
                            LibelleMoyen = eDto.LibelleMoyen,
                            PlanVerifMachinePieceRefs = eDto.PiecesRef.Select(pDto => new PlanVerifMachinePieceRef
                            {
                                PieceRefId = pDto.PieceRefId,
                                RoleVerif = pDto.RoleVerif,
                                ResultatAttendu = pDto.ResultatAttendu,
                                FamilleDesc = pDto.FamilleDesc,
                                Notes = pDto.Notes
                            }).ToList()
                        };
                        ligneEnBase.PlanVerifMachineEcheances.Add(nouvelleEcheance);
                    }
                    else
                    {
                        PlanVerifMachineMapper.MettreAJourEcheance(echeanceEnBase, eDto);

                        // --- SUPPRESSION DES PIECES (Niveau 4) ---
                        var dtoPieceIds = eDto.PiecesRef.Where(p => p.Id.HasValue).Select(p => p.Id.Value).ToList();
                        var piecesToRemove = echeanceEnBase.PlanVerifMachinePieceRefs.Where(p => !dtoPieceIds.Contains(p.Id)).ToList();
                        foreach (var p in piecesToRemove) echeanceEnBase.PlanVerifMachinePieceRefs.Remove(p);

                        // --- PARCOURS DES PIECES ---
                        foreach (var pDto in eDto.PiecesRef)
                        {
                            var isNewPiece = !pDto.Id.HasValue || pDto.Id.Value == Guid.Empty;
                            var pieceEnBase = isNewPiece ? null : echeanceEnBase.PlanVerifMachinePieceRefs.FirstOrDefault(p => p.Id == pDto.Id.Value);

                            if (pieceEnBase == null)
                            {
                                var nouvellePiece = new PlanVerifMachinePieceRef
                                {
                                    EcheanceId = echeanceEnBase.Id,
                                    PieceRefId = pDto.PieceRefId,
                                    RoleVerif = pDto.RoleVerif,
                                    ResultatAttendu = pDto.ResultatAttendu,
                                    FamilleDesc = pDto.FamilleDesc,
                                    Notes = pDto.Notes
                                };
                                echeanceEnBase.PlanVerifMachinePieceRefs.Add(nouvellePiece);
                            }
                            else
                            {
                                PlanVerifMachineMapper.MettreAJourPieceRef(pieceEnBase, pDto);
                            }
                        }
                    }
                }
            }
        }

        // 3. Logique d'archivage (ISO 9001)
        if (plan.Statut == StatutsPlan.Brouillon)
        {
            plan.Statut = StatutsPlan.Actif;
            var ancienPlanActif = await _unitOfWork.PlanVerifMachineRepository.GetPlanActifAsync(plan.MachineCode, plan.TypeRapport, plan.TypeRobinetCode);
            if (ancienPlanActif != null && ancienPlanActif.Id != plan.Id)
            {
                ancienPlanActif.Statut = StatutsPlan.Archive;
                ancienPlanActif.ModifieLe = DateTime.UtcNow;
                ancienPlanActif.CommentaireVersion = $"Archivé automatiquement suite à l'activation V{plan.Version}";
            }
        }

        await _unitOfWork.CommitAsync();
        return true;
    }

    public async Task<Guid> CreerNouvelleVersionAsync(NouvelleVersionVerifMachineDto request)
    {
        var ancienPlan = await _unitOfWork.PlanVerifMachineRepository.GetPlanAvecRelationsAsync(request.AncienId);
        if (ancienPlan == null) throw new Exception("Plan introuvable.");
        if (ancienPlan.Statut == StatutsPlan.Archive) throw new Exception("Impossible de versionner un plan archivé.");

        var nouveauPlan = PlanVerifMachineMapper.DupliquerEntitePlan(ancienPlan, request.ModifiePar, request.MotifModification);

        await _unitOfWork.PlanVerifMachineRepository.AddPlanAsync(nouveauPlan);
        await _unitOfWork.CommitAsync();

        return nouveauPlan.Id;
    }
}