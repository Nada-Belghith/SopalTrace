using SopalTrace.Application.DTOs.QualityPlans.VerifMachine;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities; // Adapter si le namespace est différent
using System;
using System.Linq;

namespace SopalTrace.Application.Mappers;

public static class PlanVerifMachineMapper
{
    // =======================================================
    // 1. MAPPING VERS DTO (GET)
    // =======================================================
    public static PlanVerifMachineResponseDto MapperEntiteVersDto(PlanVerifMachineEntete entete)
    {
        return new PlanVerifMachineResponseDto
        {
            Id = entete.Id,
            MachineCode = entete.MachineCode,
            TypeRobinetCode = entete.TypeRobinetCode,
            TypeRapport = entete.TypeRapport,
            FormulaireId = entete.FormulaireId,
            CodeFormulaire = entete.Formulaire?.CodeReference, // Join EF Core
            Nom = entete.Nom,
            Version = entete.Version,
            Statut = entete.Statut,
            CreePar = entete.CreePar,
            CreeLe = entete.CreeLe,
            ModifiePar = entete.ModifiePar,
            ModifieLe = entete.ModifieLe,
            CommentaireVersion = entete.CommentaireVersion,

            Lignes = entete.PlanVerifMachineLignes.Select(l => new VerifMachineLigneResponseDto
            {
                Id = l.Id,
                OrdreAffiche = l.OrdreAffiche,
                LibelleRisque = l.LibelleRisque,
                RisqueDefautId = l.RisqueDefautId,
                LibelleMethode = l.LibelleMethode,
                TypeSaisie = l.TypeSaisie,
                ValeurNominale = l.ValeurNominale,
                ToleranceSup = l.ToleranceSup,
                ToleranceInf = l.ToleranceInf,
                Unite = l.Unite,
                Instruction = l.Instruction,
                EstCritique = l.EstCritique,

                Echeances = l.PlanVerifMachineEcheances.Select(e => new VerifMachineEcheanceResponseDto
                {
                    Id = e.Id,
                    PeriodiciteId = e.PeriodiciteId,
                    OrdreAffiche = e.OrdreAffiche,
                    LibelleMoyen = e.LibelleMoyen,

                    PiecesRef = e.PlanVerifMachinePieceRefs.Select(p => new VerifMachinePieceRefResponseDto
                    {
                        Id = p.Id,
                        PieceRefId = p.PieceRefId,
                        RoleVerif = p.RoleVerif,
                        ResultatAttendu = p.ResultatAttendu,
                        FamilleDesc = p.FamilleDesc,
                        Notes = p.Notes
                    }).ToList()
                }).ToList()
            }).ToList()
        };
    }

    // =======================================================
    // 2. CREATION DU PLAN BROUILLON (POST)
    // =======================================================
    public static PlanVerifMachineEntete ConstruireNouveauPlan(CreatePlanVerifMachineDto dto, string creePar)
    {
        return new PlanVerifMachineEntete
        {
            Id = Guid.NewGuid(),
            MachineCode = dto.MachineCode,
            TypeRobinetCode = string.IsNullOrWhiteSpace(dto.TypeRobinetCode) ? null : dto.TypeRobinetCode,
            TypeRapport = dto.TypeRapport,
            FormulaireId = dto.FormulaireId,
            Nom = dto.Nom,
            Version = 1,
            Statut = StatutsPlan.Brouillon,
            CreePar = creePar,
            CreeLe = DateTime.UtcNow,
            CommentaireVersion = dto.CommentaireVersion ?? "Création initiale"
        };
    }

    // =======================================================
    // 3. CONSTRUCTION DYNAMIQUE DES ARBRES (PUT Valeurs)
    // =======================================================
    public static PlanVerifMachineLigne ConstruireLigneComplete(Guid planId, VerifMachineLigneEditDto dto)
    {
        var ligneId = Guid.NewGuid();
        return new PlanVerifMachineLigne
        {
            Id = ligneId,
            PlanEnteteId = planId,
            OrdreAffiche = dto.OrdreAffiche,
            LibelleRisque = dto.LibelleRisque,
            RisqueDefautId = dto.RisqueDefautId,
            LibelleMethode = dto.LibelleMethode,
            TypeSaisie = dto.TypeSaisie,
            ValeurNominale = dto.ValeurNominale,
            ToleranceSup = dto.ToleranceSup,
            ToleranceInf = dto.ToleranceInf,
            Unite = dto.Unite,
            Instruction = dto.Instruction,
            EstCritique = dto.EstCritique,
            // Cascade automatique des enfants
            PlanVerifMachineEcheances = dto.Echeances.Select(e => ConstruireEcheanceComplete(ligneId, e)).ToList()
        };
    }

    public static PlanVerifMachineEcheance ConstruireEcheanceComplete(Guid ligneId, VerifMachineEcheanceEditDto dto)
    {
        var echeanceId = Guid.NewGuid();
        return new PlanVerifMachineEcheance
        {
            Id = echeanceId,
            PlanLigneId = ligneId,
            PeriodiciteId = dto.PeriodiciteId,
            OrdreAffiche = dto.OrdreAffiche,
            LibelleMoyen = dto.LibelleMoyen,
            // Cascade automatique des enfants
            PlanVerifMachinePieceRefs = dto.PiecesRef.Select(p => ConstruirePieceRef(echeanceId, p)).ToList()
        };
    }

    public static PlanVerifMachinePieceRef ConstruirePieceRef(Guid echeanceId, VerifMachinePieceRefEditDto dto)
    {
        return new PlanVerifMachinePieceRef
        {
            Id = Guid.NewGuid(),
            EcheanceId = echeanceId,
            PieceRefId = dto.PieceRefId,
            RoleVerif = dto.RoleVerif,
            ResultatAttendu = dto.ResultatAttendu,
            FamilleDesc = dto.FamilleDesc,
            Notes = dto.Notes
        };
    }

    // =======================================================
    // 4. MISES À JOUR SIMPLES DES PROPRIÉTÉS
    // =======================================================
    public static void MettreAJourLigne(PlanVerifMachineLigne ligne, VerifMachineLigneEditDto dto)
    {
        ligne.OrdreAffiche = dto.OrdreAffiche;
        ligne.LibelleRisque = dto.LibelleRisque;
        ligne.RisqueDefautId = dto.RisqueDefautId;
        ligne.LibelleMethode = dto.LibelleMethode;
        ligne.TypeSaisie = dto.TypeSaisie;
        ligne.ValeurNominale = dto.ValeurNominale;
        ligne.ToleranceSup = dto.ToleranceSup;
        ligne.ToleranceInf = dto.ToleranceInf;
        ligne.Unite = dto.Unite;
        ligne.Instruction = dto.Instruction;
        ligne.EstCritique = dto.EstCritique;
    }

    public static void MettreAJourEcheance(PlanVerifMachineEcheance echeance, VerifMachineEcheanceEditDto dto)
    {
        echeance.PeriodiciteId = dto.PeriodiciteId;
        echeance.OrdreAffiche = dto.OrdreAffiche;
        echeance.LibelleMoyen = dto.LibelleMoyen;
    }

    public static void MettreAJourPieceRef(PlanVerifMachinePieceRef piece, VerifMachinePieceRefEditDto dto)
    {
        piece.PieceRefId = dto.PieceRefId;
        piece.RoleVerif = dto.RoleVerif;
        piece.ResultatAttendu = dto.ResultatAttendu;
        piece.FamilleDesc = dto.FamilleDesc;
        piece.Notes = dto.Notes;
    }

    // =======================================================
    // 5. VERSIONING (Clonage complet)
    // =======================================================
    public static PlanVerifMachineEntete DupliquerEntitePlan(PlanVerifMachineEntete source, string modifiePar, string motif)
    {
        var nouveauPlanId = Guid.NewGuid();
        return new PlanVerifMachineEntete
        {
            Id = nouveauPlanId,
            MachineCode = source.MachineCode,
            TypeRobinetCode = source.TypeRobinetCode,
            TypeRapport = source.TypeRapport,
            FormulaireId = source.FormulaireId,
            Nom = source.Nom,
            Version = source.Version + 1,
            Statut = StatutsPlan.Brouillon,
            CreePar = modifiePar,
            CreeLe = DateTime.UtcNow,
            CommentaireVersion = motif,

            // Clonage des 4 niveaux
            PlanVerifMachineLignes = source.PlanVerifMachineLignes.Select(l =>
            {
                var ligneId = Guid.NewGuid();
                return new PlanVerifMachineLigne
                {
                    Id = ligneId,
                    PlanEnteteId = nouveauPlanId,
                    OrdreAffiche = l.OrdreAffiche,
                    LibelleRisque = l.LibelleRisque,
                    RisqueDefautId = l.RisqueDefautId,
                    LibelleMethode = l.LibelleMethode,
                    TypeSaisie = l.TypeSaisie,
                    ValeurNominale = l.ValeurNominale,
                    ToleranceSup = l.ToleranceSup,
                    ToleranceInf = l.ToleranceInf,
                    Unite = l.Unite,
                    Instruction = l.Instruction,
                    EstCritique = l.EstCritique,

                    PlanVerifMachineEcheances = l.PlanVerifMachineEcheances.Select(e =>
                    {
                        var echeanceId = Guid.NewGuid();
                        return new PlanVerifMachineEcheance
                        {
                            Id = echeanceId,
                            PlanLigneId = ligneId,
                            PeriodiciteId = e.PeriodiciteId,
                            OrdreAffiche = e.OrdreAffiche,
                            LibelleMoyen = e.LibelleMoyen,

                            PlanVerifMachinePieceRefs = e.PlanVerifMachinePieceRefs.Select(p => new PlanVerifMachinePieceRef
                            {
                                Id = Guid.NewGuid(),
                                EcheanceId = echeanceId,
                                PieceRefId = p.PieceRefId,
                                RoleVerif = p.RoleVerif,
                                ResultatAttendu = p.ResultatAttendu,
                                FamilleDesc = p.FamilleDesc,
                                Notes = p.Notes
                            }).ToList()
                        };
                    }).ToList()
                };
            }).ToList()
        };
    }
}
