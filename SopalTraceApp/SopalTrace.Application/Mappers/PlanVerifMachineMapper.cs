using SopalTrace.Application.DTOs.QualityPlans.VerifMachine;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
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
            Nom = entete.Nom,
            Version = entete.Version ?? 0,
            Statut = entete.Statut ?? "",
            CreePar = entete.CreePar,
            CreeLe = entete.CreeLe ?? DateTime.MinValue,
            ModifiePar = entete.ModifiePar ?? string.Empty,
            ModifieLe = entete.ModifieLe,

            // ✅ CORRECTION ICI : Ajout de "?? false" pour gérer le bool? de la base de données
            AfficheConformite = entete.AfficheConformite ?? false,
            AfficheMoyenDetectionRisques = entete.AfficheMoyenDetectionRisques ?? false,
            AfficheFamilles = entete.AfficheFamilles ?? false,
            AfficheFuiteEtalon = entete.AfficheFuiteEtalon ?? false,

            Familles = entete.PlanVerifMachineFamilles?.Select(f => new VmFamilleDto
            {
                Id = f.Id,
                RefFamilleCorpsId = f.RefFamilleCorpsId,
                OrdreAffiche = f.OrdreAffiche
            }).ToList() ?? new List<VmFamilleDto>(),

            LignesConformite = entete.PlanVerifMachineLignes
                .Where(l => l.TypeLigne == "CONFORMITE")
                .OrderBy(l => l.OrdreAffiche)
                .Select(MapperLigneVersDto)
                .ToList(),

            LignesRisques = entete.PlanVerifMachineLignes
                .Where(l => l.TypeLigne == "RISQUE")
                .OrderBy(l => l.OrdreAffiche)
                .Select(MapperLigneVersDto)
                .ToList()
        };
    }

    private static VmLigneResponseDto MapperLigneVersDto(PlanVerifMachineLigne ligne)
    {
        return new VmLigneResponseDto
        {
            Id = ligne.Id,
            OrdreAffiche = ligne.OrdreAffiche,
            TypeLigne = ligne.TypeLigne,
            LibelleRisque = ligne.LibelleRisque,
            LibelleMethode = ligne.LibelleMethode,
            Echeances = ligne.PlanVerifMachineEcheances
                .OrderBy(e => e.OrdreAffiche)
                .Select(e => new VmEcheanceResponseDto
                {
                    Id = e.Id,
                    PeriodiciteId = e.PeriodiciteId,
                    OrdreAffiche = e.OrdreAffiche,
                    RefMoyenDetectionId = e.RefMoyenDetectionId, // ✅ 100% Dictionnaire
                    PiecesRef = e.PlanVerifMachineMatricePieces.Select(p => new VmPieceRefResponseDto
                    {
                        Id = p.Id,
                        PieceRefId = p.PieceRefId,
                        RoleVerif = p.RoleVerif ?? "",
                        FamilleId = p.Famille?.RefFamilleCorpsId
                    }).ToList()
                }).ToList()
        };
    }

    // =======================================================
    // 2. CONSTRUCTION DU PLAN (POST)
    // =======================================================
    public static PlanVerifMachineEntete ConstruireDepuisModeleDto(CreateVerifMachineModeleDto dto, string creePar)
    {
        var planId = Guid.NewGuid();

        var entete = new PlanVerifMachineEntete
        {
            Id = planId,
            MachineCode = dto.MachineCode,
            Nom = dto.Nom,
            Version = 1,
            Statut = StatutsPlan.Actif,
            CreePar = creePar,
            CreeLe = DateTime.UtcNow,
            
            // Flags
            AfficheConformite = dto.AfficheConformite,
            AfficheMoyenDetectionRisques = dto.AfficheMoyenDetectionRisques,
            AfficheFamilles = dto.AfficheFamilles,
            AfficheFuiteEtalon = dto.AfficheFuiteEtalon
        };

        var famillesDb = dto.Familles?.Select(f => new PlanVerifMachineFamille
        {
            Id = Guid.NewGuid(),
            PlanEnteteId = planId,
            RefFamilleCorpsId = f.RefFamilleCorpsId,
            OrdreAffiche = f.OrdreAffiche
        }).ToList() ?? new List<PlanVerifMachineFamille>();

        entete.PlanVerifMachineFamilles = famillesDb;

        // Lignes
        var lignes = dto.LignesConformite.Select((l, idx) => ConstruireLigne(planId, l, "CONFORMITE", famillesDb))
            .Concat(dto.LignesRisques.Select((l, idx) => ConstruireLigne(planId, l, "RISQUE", famillesDb)))
            .ToList();

        entete.PlanVerifMachineLignes = lignes;
        return entete;
    }

    private static PlanVerifMachineLigne ConstruireLigne(Guid planId, VmLigneDto dto, string typeLigne, List<PlanVerifMachineFamille> famillesDb)
    {
        var ligneId = Guid.NewGuid();
        return new PlanVerifMachineLigne
        {
            Id = ligneId,
            PlanEnteteId = planId,
            OrdreAffiche = dto.OrdreAffiche,
            LibelleRisque = dto.LibelleRisque,
            LibelleMethode = dto.LibelleMethode,
            TypeLigne = typeLigne,
            PlanVerifMachineEcheances = dto.Echeances.Select(e => ConstruireEcheance(ligneId, e, famillesDb)).ToList()
        };
    }

    private static PlanVerifMachineEcheance ConstruireEcheance(Guid ligneId, VmEcheanceDto dto, List<PlanVerifMachineFamille> famillesDb)
    {
        var echeanceId = Guid.NewGuid();
        return new PlanVerifMachineEcheance
        {
            Id = echeanceId,
            PlanLigneId = ligneId,
            OrdreAffiche = dto.OrdreAffiche,
            PeriodiciteId = dto.PeriodiciteId, // ✅ Direct, plus de Guid.TryParse
            RefMoyenDetectionId = dto.RefMoyenDetectionId, // ✅ Direct
            PlanVerifMachineMatricePieces = dto.MatricePieces.Select(p => {
                
                // Résolution de l'ID interne de la famille depuis l'ID du dictionnaire
                var familleAssociee = p.FamilleId.HasValue 
                    ? famillesDb.FirstOrDefault(f => f.RefFamilleCorpsId == p.FamilleId.Value) 
                    : null;

                return new PlanVerifMachineMatricePiece
                {
                    Id = Guid.NewGuid(),
                    EcheanceId = echeanceId,
                    PieceRefId = p.PieceRefId, // Nullable pour les champs vides
                    RoleVerif = p.RoleVerif,
                    FamilleId = familleAssociee?.Id
                };
            }).ToList()
        };
    }

    // =======================================================
    // 3. VERSIONING ET DUPLICATION
    // =======================================================
    public static CreateVerifMachineModeleDto MapperEntiteVersModeleDto(PlanVerifMachineEntete entete)
    {
        return new CreateVerifMachineModeleDto
        {
            Nom = entete.Nom,
            MachineCode = entete.MachineCode,
            AfficheConformite = entete.AfficheConformite ?? false,
            AfficheMoyenDetectionRisques = entete.AfficheMoyenDetectionRisques ?? false,
            AfficheFamilles = entete.AfficheFamilles ?? false,
            AfficheFuiteEtalon = entete.AfficheFuiteEtalon ?? false,
            Familles = entete.PlanVerifMachineFamilles?.Select(f => new VmFamilleDto
            {
                RefFamilleCorpsId = f.RefFamilleCorpsId,
                OrdreAffiche = f.OrdreAffiche
            }).ToList() ?? new List<VmFamilleDto>(),
            LignesConformite = entete.PlanVerifMachineLignes
                .Where(l => l.TypeLigne == "CONFORMITE")
                .OrderBy(l => l.OrdreAffiche)
                .Select(MapperLigneVersCreateDto).ToList(),
            LignesRisques = entete.PlanVerifMachineLignes
                .Where(l => l.TypeLigne == "RISQUE")
                .OrderBy(l => l.OrdreAffiche)
                .Select(MapperLigneVersCreateDto).ToList()
        };
    }

    private static VmLigneDto MapperLigneVersCreateDto(PlanVerifMachineLigne ligne)
    {
        return new VmLigneDto
        {
            OrdreAffiche = ligne.OrdreAffiche,
            LibelleRisque = ligne.LibelleRisque,
            LibelleMethode = ligne.LibelleMethode,
            Echeances = ligne.PlanVerifMachineEcheances
                .OrderBy(e => e.OrdreAffiche)
                .Select(e => new VmEcheanceDto
                {
                    OrdreAffiche = e.OrdreAffiche,
                    PeriodiciteId = e.PeriodiciteId,
                    RefMoyenDetectionId = e.RefMoyenDetectionId,
                    MatricePieces = e.PlanVerifMachineMatricePieces.Select(mp => new VmPieceRefDto
                    {
                        FamilleId = mp.Famille?.RefFamilleCorpsId,
                        RoleVerif = mp.RoleVerif,
                        PieceRefId = mp.PieceRefId
                    }).ToList()
                }).ToList()
        };
    }

    public static PlanVerifMachineEntete DupliquerEntitePlan(PlanVerifMachineEntete source, string modifiePar, string motif)
    {
        // On convertit en DTO puis on reconstruit (méthode la plus sûre pour cloner tout l'arbre)
        var dto = MapperEntiteVersModeleDto(source);
        var nouveauPlan = ConstruireDepuisModeleDto(dto, modifiePar);
        
        nouveauPlan.Version = (source.Version ?? 1) + 1;
        nouveauPlan.Statut = StatutsPlan.Actif;
        return nouveauPlan;
    }

    // Compatibilité pour l'ancien contrôleur
    public static PlanVerifMachineEntete ConstruireNouveauPlan(CreatePlanVerifMachineDto dto, string creePar)
    {
        return new PlanVerifMachineEntete
        {
            Id = Guid.NewGuid(),
            MachineCode = dto.MachineCode,
            Nom = dto.Nom,
            Version = 1,
            Statut = StatutsPlan.Brouillon,
            CreePar = creePar,
            CreeLe = DateTime.UtcNow
        };
    }
}