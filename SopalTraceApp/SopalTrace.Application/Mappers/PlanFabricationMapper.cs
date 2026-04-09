#nullable disable

using SopalTrace.Application.DTOs.QualityPlans.Modeles;
using SopalTrace.Application.DTOs.QualityPlans.Plans;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities; // <-- LA CORRECTION EST ICI !
using System;
using System.Collections.Generic;
using System.Linq;

namespace SopalTrace.Application.Mappers;

public static class PlanFabricationMapper
{
    // ====================================================================
    // 1. CRÉATION INITIALE : MODÈLES ET PLANS (POST)
    // ====================================================================

    public static ModeleFabEntete ConstruireEntiteModeleAPartirDeDto(CreateModeleRequestDto dto)
    {
        var modele = new ModeleFabEntete
        {
            Id = Guid.NewGuid(),
            Code = dto.Code,
            Libelle = dto.Libelle,
            TypeRobinetCode = dto.TypeRobinetCode,
            NatureComposantCode = dto.NatureComposantCode,
            OperationCode = dto.OperationCode,
            Version = 1,
            Statut = StatutsPlan.Actif,
            Notes = dto.Notes,
            CreePar = "Admin",
            CreeLe = DateTime.UtcNow,
            ModeleFabSections = new List<ModeleFabSection>()
        };

        foreach (var sectionDto in dto.Sections)
        {
            var section = new ModeleFabSection
            {
                Id = Guid.NewGuid(),
                ModeleEnteteId = modele.Id,
                OrdreAffiche = sectionDto.OrdreAffiche,
                LibelleSection = sectionDto.LibelleSection,
                FrequenceLibelle = sectionDto.FrequenceLibelle,
                ModeleFabLignes = new List<ModeleFabLigne>()
            };

            foreach (var ligneDto in sectionDto.Lignes)
            {
                var ligne = new ModeleFabLigne
                {
                    Id = Guid.NewGuid(),
                    ModeleEnteteId = modele.Id,
                    SectionId = section.Id,
                    OrdreAffiche = ligneDto.OrdreAffiche,
                    OutilSourceId = ligneDto.OutilSourceId,
                    TypeCaracteristiqueId = ligneDto.TypeCaracteristiqueId,
                    LibelleAffiche = string.IsNullOrWhiteSpace(ligneDto.LibelleAffiche) ? null : ligneDto.LibelleAffiche,
                    TypeControleId = ligneDto.TypeControleId,
                    MoyenControleId = ligneDto.MoyenControleId,
                    GroupeInstrumentId = ligneDto.GroupeInstrumentId,
                    PeriodiciteId = ligneDto.PeriodiciteId,
                    Instruction = string.IsNullOrWhiteSpace(ligneDto.Instruction) ? null : ligneDto.Instruction,
                    EstCritique = ligneDto.EstCritique
                };

                section.ModeleFabLignes.Add(ligne);
            }
            modele.ModeleFabSections.Add(section);
        }

        return modele;
    }

    public static PlanFabEntete ConstruireEntitePlanAPartirDeModele(ModeleFabEntete modele, CreatePlanRequestDto dto, string designationSage)
    {
        var plan = new PlanFabEntete
        {
            Id = Guid.NewGuid(),
            ModeleSourceId = modele.Id,
            CodeArticleSage = dto.CodeArticleSage,
            Designation = designationSage,
            Nom = dto.Nom ?? $"PC-{dto.CodeArticleSage}-v1",
            Version = 1,
            Statut = StatutsPlan.Brouillon,
            CreePar = "Admin",
            CreeLe = DateTime.UtcNow,
            CommentaireVersion = dto.CommentaireVersion,
            PlanFabSections = new List<PlanFabSection>()
        };

        foreach (var modeleSection in modele.ModeleFabSections)
        {
            var planSection = new PlanFabSection
            {
                Id = Guid.NewGuid(),
                PlanEnteteId = plan.Id,
                ModeleSectionId = modeleSection.Id,
                OrdreAffiche = modeleSection.OrdreAffiche,
                LibelleSection = modeleSection.LibelleSection,
                FrequenceLibelle = modeleSection.FrequenceLibelle,
                PlanFabLignes = new List<PlanFabLigne>()
            };

            foreach (var modeleLigne in modeleSection.ModeleFabLignes)
            {
                var planLigne = new PlanFabLigne
                {
                    Id = Guid.NewGuid(),
                    PlanEnteteId = plan.Id,
                    SectionId = planSection.Id,
                    ModeleLigneSourceId = modeleLigne.Id,
                    OrdreAffiche = modeleLigne.OrdreAffiche,
                    OutilSourceId = modeleLigne.OutilSourceId,
                    TypeCaracteristiqueId = modeleLigne.TypeCaracteristiqueId,
                    LibelleAffiche = modeleLigne.LibelleAffiche,
                    TypeControleId = modeleLigne.TypeControleId,
                    MoyenControleId = modeleLigne.MoyenControleId,
                    GroupeInstrumentId = modeleLigne.GroupeInstrumentId,
                    PeriodiciteId = modeleLigne.PeriodiciteId,
                    Instruction = modeleLigne.Instruction,
                    EstCritique = modeleLigne.EstCritique
                };

                planSection.PlanFabLignes.Add(planLigne);
            }
            plan.PlanFabSections.Add(planSection);
        }

        return plan;
    }

    // ====================================================================
    // 2. CLONAGE ET VERSIONING (POST)
    // ====================================================================

    public static PlanFabEntete DupliquerEntitePlan(PlanFabEntete source, string nouveauCode, string nouvelleDesig, string creePar, string comm = null)
    {
        var plan = new PlanFabEntete
        {
            Id = Guid.NewGuid(),
            ModeleSourceId = source.ModeleSourceId,
            CodeArticleSage = nouveauCode,
            Designation = nouvelleDesig,
            Nom = source.Nom,
            Version = comm == null ? 1 : source.Version + 1,
            Statut = StatutsPlan.Brouillon,
            MachineDefautCode = source.MachineDefautCode,
            CreePar = creePar,
            CreeLe = DateTime.UtcNow,
            CommentaireVersion = comm ?? $"Cloné à partir de l'article {source.CodeArticleSage}",
            PlanFabSections = new List<PlanFabSection>()
        };

        if (comm == null) plan.Nom = $"PC-{nouveauCode}-v1";

        foreach (var sourceSection in source.PlanFabSections)
        {
            var planSection = new PlanFabSection
            {
                Id = Guid.NewGuid(),
                PlanEnteteId = plan.Id,
                ModeleSectionId = sourceSection.ModeleSectionId,
                OrdreAffiche = sourceSection.OrdreAffiche,
                LibelleSection = sourceSection.LibelleSection,
                FrequenceLibelle = sourceSection.FrequenceLibelle,
                PlanFabLignes = new List<PlanFabLigne>()
            };

            foreach (var sourceLigne in sourceSection.PlanFabLignes)
            {
                var planLigne = new PlanFabLigne
                {
                    Id = Guid.NewGuid(),
                    PlanEnteteId = plan.Id,
                    SectionId = planSection.Id,
                    ModeleLigneSourceId = sourceLigne.ModeleLigneSourceId,
                    OrdreAffiche = sourceLigne.OrdreAffiche,
                    OutilSourceId = sourceLigne.OutilSourceId,
                    TypeCaracteristiqueId = sourceLigne.TypeCaracteristiqueId,
                    LibelleAffiche = sourceLigne.LibelleAffiche,
                    TypeControleId = sourceLigne.TypeControleId,
                    MoyenControleId = sourceLigne.MoyenControleId,
                    GroupeInstrumentId = sourceLigne.GroupeInstrumentId,
                    InstrumentCode = sourceLigne.InstrumentCode,
                    PeriodiciteId = sourceLigne.PeriodiciteId,
                    ValeurNominale = sourceLigne.ValeurNominale,
                    ToleranceSuperieure = sourceLigne.ToleranceSuperieure,
                    ToleranceInferieure = sourceLigne.ToleranceInferieure,
                    Unite = sourceLigne.Unite,
                    LimiteSpecTexte = sourceLigne.LimiteSpecTexte,
                    Observations = sourceLigne.Observations,
                    Instruction = sourceLigne.Instruction,
                    EstCritique = sourceLigne.EstCritique
                };
                planSection.PlanFabLignes.Add(planLigne);
            }
            plan.PlanFabSections.Add(planSection);
        }

        return plan;
    }

    // ====================================================================
    // 3. LIBERTÉ TOTALE : MISE À JOUR ET AJOUT DYNAMIQUE (PUT)
    // ====================================================================

    public static PlanFabSection ConstruireNouvelleSectionPlan(Guid planId, SectionEditDto dto)
    {
        return new PlanFabSection
        {
            // FIX CRUCIAL : L'Id n'est pas assigné manuellement ici. 
            // EF Core génèrera lui-même la clé et effectuera un INSERT.
            PlanEnteteId = planId,
            OrdreAffiche = dto.OrdreAffiche,
            LibelleSection = string.IsNullOrWhiteSpace(dto.LibelleSection) ? "NOUVELLE SECTION" : dto.LibelleSection,
            PlanFabLignes = new List<PlanFabLigne>()
        };
    }

    public static PlanFabLigne ConstruireNouvelleLignePlan(Guid planId, Guid sectionId, LigneEditDto dto)
    {
        return new PlanFabLigne
        {
            // FIX CRUCIAL : L'Id n'est pas assigné manuellement ici.
            PlanEnteteId = planId,
            SectionId = sectionId,
            OrdreAffiche = dto.OrdreAffiche,

            OutilSourceId = dto.OutilSourceId,
            TypeCaracteristiqueId = dto.TypeCaracteristiqueId,
            TypeControleId = dto.TypeControleId,
            LibelleAffiche = string.IsNullOrWhiteSpace(dto.LibelleAffiche) ? null : dto.LibelleAffiche,
            EstCritique = dto.EstCritique,
            MoyenControleId = dto.MoyenControleId,
            GroupeInstrumentId = dto.GroupeInstrumentId,
            InstrumentCode = string.IsNullOrWhiteSpace(dto.InstrumentCode) ? null : dto.InstrumentCode,
            PeriodiciteId = dto.PeriodiciteId,
            ValeurNominale = dto.ValeurNominale,
            ToleranceSuperieure = dto.ToleranceSuperieure,
            ToleranceInferieure = dto.ToleranceInferieure,
            Unite = string.IsNullOrWhiteSpace(dto.Unite) ? null : dto.Unite,
            LimiteSpecTexte = string.IsNullOrWhiteSpace(dto.LimiteSpecTexte) ? null : dto.LimiteSpecTexte,
            Observations = string.IsNullOrWhiteSpace(dto.Observations) ? null : dto.Observations,
            Instruction = string.IsNullOrWhiteSpace(dto.Instruction) ? null : dto.Instruction
        };
    }

    public static void MettreAJourEntiteLigne(PlanFabLigne ligne, LigneEditDto dto)
    {
        ligne.OrdreAffiche = dto.OrdreAffiche;

        ligne.OutilSourceId = dto.OutilSourceId;
        ligne.TypeCaracteristiqueId = dto.TypeCaracteristiqueId;
        ligne.TypeControleId = dto.TypeControleId;
        ligne.LibelleAffiche = string.IsNullOrWhiteSpace(dto.LibelleAffiche) ? null : dto.LibelleAffiche;
        ligne.EstCritique = dto.EstCritique;
        ligne.MoyenControleId = dto.MoyenControleId;
        ligne.GroupeInstrumentId = dto.GroupeInstrumentId;
        ligne.InstrumentCode = string.IsNullOrWhiteSpace(dto.InstrumentCode) ? null : dto.InstrumentCode;
        ligne.PeriodiciteId = dto.PeriodiciteId;
        ligne.ValeurNominale = dto.ValeurNominale;
        ligne.ToleranceSuperieure = dto.ToleranceSuperieure;
        ligne.ToleranceInferieure = dto.ToleranceInferieure;
        ligne.Unite = string.IsNullOrWhiteSpace(dto.Unite) ? null : dto.Unite;
        ligne.LimiteSpecTexte = string.IsNullOrWhiteSpace(dto.LimiteSpecTexte) ? null : dto.LimiteSpecTexte;
        ligne.Observations = string.IsNullOrWhiteSpace(dto.Observations) ? null : dto.Observations;
        ligne.Instruction = string.IsNullOrWhiteSpace(dto.Instruction) ? null : dto.Instruction;
    }

    // ====================================================================
    // 4. MAPPINGS VERS DTOs : AFFICHAGE DES RÉSULTATS (GET)
    // ====================================================================

    public static ModeleResponseDto MapperEntiteModeleVersDto(ModeleFabEntete modele)
    {
        return new ModeleResponseDto
        {
            Id = modele.Id,
            Code = modele.Code,
            Libelle = modele.Libelle,
            TypeRobinetCode = modele.TypeRobinetCode,
            NatureComposantCode = modele.NatureComposantCode,
            OperationCode = modele.OperationCode,
            Version = modele.Version,
            Statut = modele.Statut,
            Notes = modele.Notes ?? string.Empty,
            CreePar = modele.CreePar,
            CreeLe = modele.CreeLe,
            ArchiveLe = modele.ArchiveLe,
            ArchivePar = modele.ArchivePar ?? string.Empty,
            Sections = modele.ModeleFabSections.Select(s => new ModeleSectionResponseDto
            {
                Id = s.Id,
                OrdreAffiche = s.OrdreAffiche,
                LibelleSection = s.LibelleSection,
                FrequenceLibelle = s.FrequenceLibelle ?? string.Empty,
                Lignes = s.ModeleFabLignes.Select(l => new ModeleLigneResponseDto
                {
                    Id = l.Id,
                    OrdreAffiche = l.OrdreAffiche,
                    OutilSourceId = l.OutilSourceId,
                    TypeCaracteristiqueId = l.TypeCaracteristiqueId,
                    LibelleAffiche = l.LibelleAffiche ?? string.Empty,
                    TypeControleId = l.TypeControleId,
                    MoyenControleId = l.MoyenControleId,
                    GroupeInstrumentId = l.GroupeInstrumentId,
                    PeriodiciteId = l.PeriodiciteId,
                    Instruction = l.Instruction ?? string.Empty,
                    EstCritique = l.EstCritique
                }).ToList()
            }).ToList()
        };
    }

    public static PlanResponseDto MapperEntitePlanVersDto(PlanFabEntete plan)
    {
        return new PlanResponseDto
        {
            Id = plan.Id,
            ModeleSourceId = plan.ModeleSourceId,
            CodeArticleSage = plan.CodeArticleSage,
            Nom = plan.Nom,
            Designation = plan.Designation ?? string.Empty,
            Version = plan.Version,
            Statut = plan.Statut,
            DateApplication = plan.DateApplication?.ToDateTime(TimeOnly.MinValue),
            MachineDefautCode = plan.MachineDefautCode,
            CreePar = plan.CreePar,
            CreeLe = plan.CreeLe,
            ModifiePar = plan.ModifiePar ?? string.Empty,
            ModifieLe = plan.ModifieLe,
            CommentaireVersion = plan.CommentaireVersion ?? string.Empty,
            Sections = plan.PlanFabSections.Select(s => new PlanSectionResponseDto
            {
                Id = s.Id,
                ModeleSectionId = s.ModeleSectionId,
                OrdreAffiche = s.OrdreAffiche,
                LibelleSection = s.LibelleSection,
                FrequenceLibelle = s.FrequenceLibelle ?? string.Empty,
                Lignes = s.PlanFabLignes.Select(l => new PlanLigneResponseDto
                {
                    Id = l.Id,
                    ModeleLigneSourceId = l.ModeleLigneSourceId,
                    OrdreAffiche = l.OrdreAffiche,
                    OutilSourceId = l.OutilSourceId,
                    TypeCaracteristiqueId = l.TypeCaracteristiqueId,
                    LibelleAffiche = l.LibelleAffiche ?? string.Empty,
                    TypeControleId = l.TypeControleId,
                    MoyenControleId = l.MoyenControleId,
                    GroupeInstrumentId = l.GroupeInstrumentId,
                    InstrumentCode = l.InstrumentCode ?? string.Empty,
                    PeriodiciteId = l.PeriodiciteId,
                    ValeurNominale = l.ValeurNominale,
                    ToleranceSuperieure = l.ToleranceSuperieure,
                    ToleranceInferieure = l.ToleranceInferieure,
                    Unite = l.Unite ?? string.Empty,
                    LimiteSpecTexte = l.LimiteSpecTexte ?? string.Empty,
                    Observations = l.Observations ?? string.Empty,
                    Instruction = l.Instruction ?? string.Empty,
                    EstCritique = l.EstCritique
                }).ToList()
            }).ToList()
        };
    }
}