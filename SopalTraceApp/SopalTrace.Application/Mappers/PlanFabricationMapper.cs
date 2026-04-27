#nullable enable

using SopalTrace.Application.DTOs.QualityPlans.PlanFabrication;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SopalTrace.Application.Mappers;

public static class PlanFabricationMapper
{
    private static Guid? NullIfEmpty(Guid? value) => value == Guid.Empty ? null : value;

    private static bool IsCustomInstrumentCode(string? instrumentCode)
        => !string.IsNullOrWhiteSpace(instrumentCode)
           && instrumentCode.Any(ch => !char.IsLetterOrDigit(ch) && ch != '-' && ch != '_' && ch != '/');

    private static (string? InstrumentCode, string? MoyenTexteLibre) NormalizeInstrumentCode(string? instrumentCode, string? moyenTexteLibre = null)
    {
        var normalizedInstrumentCode = string.IsNullOrWhiteSpace(instrumentCode) ? null : instrumentCode.Trim();
        var normalizedMoyenTexteLibre = string.IsNullOrWhiteSpace(moyenTexteLibre) ? null : moyenTexteLibre.Trim();

        if (!string.IsNullOrWhiteSpace(normalizedMoyenTexteLibre))
        {
            return (normalizedInstrumentCode, normalizedMoyenTexteLibre);
        }

        if (IsCustomInstrumentCode(normalizedInstrumentCode))
        {
            return (null, normalizedInstrumentCode);
        }

        return (normalizedInstrumentCode, null);
    }


    public static PlanFabEntete ConstruireEntitePlanAPartirDeModele(ModeleFabEntete modele, CreatePlanRequestDto dto, string designationSage)
    {
        var plan = new PlanFabEntete
        {
            Id = Guid.NewGuid(), ModeleSourceId = modele.Id, CodeArticleSage = dto.CodeArticleSage,
            Designation = designationSage, Nom = dto.Nom ?? $"PC-{dto.CodeArticleSage}",
            Version = 1, Statut = StatutsPlan.Brouillon, CreePar = "Admin", CreeLe = DateTime.UtcNow,
            OperationCode = string.IsNullOrWhiteSpace(dto.OperationCode) ? modele.OperationCode : dto.OperationCode,
            CommentaireVersion = dto.CommentaireVersion,
            LegendeMoyens = string.IsNullOrWhiteSpace(dto.LegendeMoyens) ? modele.LegendeMoyens : dto.LegendeMoyens,
            PlanFabSections = new List<PlanFabSection>()
        };

        foreach (var modeleSection in modele.ModeleFabSections)
        {
            var planSection = new PlanFabSection
            {
                Id = Guid.NewGuid(), PlanEnteteId = plan.Id, ModeleSectionId = modeleSection.Id,
                OrdreAffiche = modeleSection.OrdreAffiche, LibelleSection = modeleSection.LibelleSection,
                FrequenceLibelle = modeleSection.FrequenceLibelle, PlanFabLignes = new List<PlanFabLigne>()
            };

            foreach (var modeleLigne in modeleSection.ModeleFabLignes)
            {
                var instrumentData = NormalizeInstrumentCode(modeleLigne.InstrumentCode, modeleLigne.MoyenTexteLibre);

                planSection.PlanFabLignes.Add(new PlanFabLigne
                {
                    Id = Guid.NewGuid(), PlanEnteteId = plan.Id, SectionId = planSection.Id, ModeleLigneSourceId = modeleLigne.Id,
                    OrdreAffiche = modeleLigne.OrdreAffiche, TypeCaracteristiqueId = modeleLigne.TypeCaracteristiqueId,
                    LibelleAffiche = modeleLigne.LibelleAffiche, TypeControleId = modeleLigne.TypeControleId,
                    MoyenControleId = NullIfEmpty(modeleLigne.MoyenControleId),
                    InstrumentCode = instrumentData.InstrumentCode,
                    MoyenTexteLibre = instrumentData.MoyenTexteLibre,
                    PeriodiciteId = NullIfEmpty(modeleLigne.PeriodiciteId), Instruction = modeleLigne.Instruction, EstCritique = modeleLigne.EstCritique,
                    ValeurNominale = modeleLigne.ValeurNominale, ToleranceSuperieure = modeleLigne.ToleranceSuperieure,
                    ToleranceInferieure = modeleLigne.ToleranceInferieure,
                    LimiteSpecTexte = string.IsNullOrWhiteSpace(modeleLigne.LimiteSpecTexte) ? null : modeleLigne.LimiteSpecTexte
                });
            }
            plan.PlanFabSections.Add(planSection);
        }

        return plan;
    }

    public static PlanFabEntete DupliquerEntitePlan(PlanFabEntete source, string nouveauCode, string nouvelleDesig, string? creePar, string? comm = null)
    {
        int nouvelleVersion = comm == null ? 1 : source.Version + 1;
        var plan = new PlanFabEntete
        {
            Id = Guid.NewGuid(), ModeleSourceId = source.ModeleSourceId, CodeArticleSage = nouveauCode,
            Designation = nouvelleDesig, Nom = comm == null ? $"PC-{nouveauCode}" : ModeleFabricationMapper.IncrementerSuffixeVersion(source.Nom, nouvelleVersion),
            Version = nouvelleVersion, Statut = StatutsPlan.Brouillon, MachineDefautCode = source.MachineDefautCode,
            OperationCode = source.OperationCode, // ⚠️ COPIE OBLIGATOIRE
            LegendeMoyens = source.LegendeMoyens,
            CreePar = creePar ?? "SYSTEM", CreeLe = DateTime.UtcNow, CommentaireVersion = comm ?? $"Cloné à partir de l'article {source.CodeArticleSage}",
            PlanFabSections = new List<PlanFabSection>()
        };

        foreach (var sourceSection in source.PlanFabSections)
        {
            var planSection = new PlanFabSection
            {
                Id = Guid.NewGuid(), PlanEnteteId = plan.Id, ModeleSectionId = sourceSection.ModeleSectionId,
                OrdreAffiche = sourceSection.OrdreAffiche, LibelleSection = sourceSection.LibelleSection,
                FrequenceLibelle = sourceSection.FrequenceLibelle, PlanFabLignes = new List<PlanFabLigne>()
            };

            foreach (var sourceLigne in sourceSection.PlanFabLignes)
            {
                planSection.PlanFabLignes.Add(new PlanFabLigne
                {
                    Id = Guid.NewGuid(), PlanEnteteId = plan.Id, SectionId = planSection.Id, ModeleLigneSourceId = sourceLigne.ModeleLigneSourceId,
                    OrdreAffiche = sourceLigne.OrdreAffiche, TypeCaracteristiqueId = sourceLigne.TypeCaracteristiqueId,
                    LibelleAffiche = sourceLigne.LibelleAffiche, TypeControleId = sourceLigne.TypeControleId,
                    MoyenControleId = NullIfEmpty(sourceLigne.MoyenControleId), PeriodiciteId = NullIfEmpty(sourceLigne.PeriodiciteId),
                    InstrumentCode = sourceLigne.InstrumentCode,
                    MoyenTexteLibre = sourceLigne.MoyenTexteLibre,
                    ValeurNominale = sourceLigne.ValeurNominale, ToleranceSuperieure = sourceLigne.ToleranceSuperieure,
                    ToleranceInferieure = sourceLigne.ToleranceInferieure, Unite = sourceLigne.Unite, LimiteSpecTexte = sourceLigne.LimiteSpecTexte,
                    Observations = sourceLigne.Observations, Instruction = sourceLigne.Instruction, EstCritique = sourceLigne.EstCritique
                });
            }
            plan.PlanFabSections.Add(planSection);
        }
        return plan;
    }

    public static PlanFabSection ConstruireNouvelleSectionPlan(Guid planId, SectionEditDto dto)
    {
        return new PlanFabSection
        {
            PlanEnteteId = planId, ModeleSectionId = NullIfEmpty(dto.ModeleSectionId), OrdreAffiche = dto.OrdreAffiche,
            LibelleSection = string.IsNullOrWhiteSpace(dto.LibelleSection) ? "NOUVELLE SECTION" : dto.LibelleSection,
            FrequenceLibelle = string.IsNullOrWhiteSpace(dto.FrequenceLibelle) ? null : dto.FrequenceLibelle,
            PlanFabLignes = new List<PlanFabLigne>()
        };
    }

    public static PlanFabLigne ConstruireNouvelleLignePlan(Guid planId, Guid sectionId, LigneEditDto dto)
    {
        var instrumentData = NormalizeInstrumentCode(dto.InstrumentCode);

        return new PlanFabLigne
        {
            PlanEnteteId = planId, SectionId = sectionId, ModeleLigneSourceId = NullIfEmpty(dto.ModeleLigneSourceId),
            OrdreAffiche = dto.OrdreAffiche, 
            TypeCaracteristiqueId = NullIfEmpty(dto.TypeCaracteristiqueId), 
            TypeControleId = NullIfEmpty(dto.TypeControleId),
            LibelleAffiche = string.IsNullOrWhiteSpace(dto.LibelleAffiche) ? null : dto.LibelleAffiche, EstCritique = dto.EstCritique,
            MoyenControleId = NullIfEmpty(dto.MoyenControleId), PeriodiciteId = NullIfEmpty(dto.PeriodiciteId),
            InstrumentCode = instrumentData.InstrumentCode, MoyenTexteLibre = instrumentData.MoyenTexteLibre, ValeurNominale = dto.ValeurNominale,
            ToleranceSuperieure = dto.ToleranceSuperieure, ToleranceInferieure = dto.ToleranceInferieure, Unite = string.IsNullOrWhiteSpace(dto.Unite) ? null : dto.Unite,
            LimiteSpecTexte = string.IsNullOrWhiteSpace(dto.LimiteSpecTexte) ? null : dto.LimiteSpecTexte, Observations = string.IsNullOrWhiteSpace(dto.Observations) ? null : dto.Observations,
            Instruction = string.IsNullOrWhiteSpace(dto.Instruction) ? null : dto.Instruction
        };
    }

    public static void MettreAJourEntiteLigne(PlanFabLigne ligne, LigneEditDto dto)
    {
        var instrumentData = NormalizeInstrumentCode(dto.InstrumentCode);

        ligne.OrdreAffiche = dto.OrdreAffiche; 
        ligne.TypeCaracteristiqueId = NullIfEmpty(dto.TypeCaracteristiqueId); 
        ligne.TypeControleId = NullIfEmpty(dto.TypeControleId);
        ligne.LibelleAffiche = string.IsNullOrWhiteSpace(dto.LibelleAffiche) ? null : dto.LibelleAffiche; ligne.EstCritique = dto.EstCritique;
        ligne.MoyenControleId = NullIfEmpty(dto.MoyenControleId); ligne.PeriodiciteId = NullIfEmpty(dto.PeriodiciteId);
        ligne.InstrumentCode = instrumentData.InstrumentCode; ligne.MoyenTexteLibre = instrumentData.MoyenTexteLibre; ligne.ValeurNominale = dto.ValeurNominale;
        ligne.ToleranceSuperieure = dto.ToleranceSuperieure; ligne.ToleranceInferieure = dto.ToleranceInferieure; ligne.Unite = string.IsNullOrWhiteSpace(dto.Unite) ? null : dto.Unite;
        ligne.LimiteSpecTexte = string.IsNullOrWhiteSpace(dto.LimiteSpecTexte) ? null : dto.LimiteSpecTexte; ligne.Observations = string.IsNullOrWhiteSpace(dto.Observations) ? null : dto.Observations; ligne.Instruction = string.IsNullOrWhiteSpace(dto.Instruction) ? null : dto.Instruction;
    }

    public static PlanResponseDto MapperEntitePlanVersDto(PlanFabEntete plan)
    {
        return new PlanResponseDto
        {
            Id = plan.Id,
            ModeleSourceId = plan.ModeleSourceId ?? Guid.Empty,
            OperationCode = !string.IsNullOrWhiteSpace(plan.OperationCode) ? plan.OperationCode : plan.ModeleSource?.OperationCode ?? string.Empty, // << PRISE EN COMPTE DU NOYAU SAUVEGARDÉ
            CodeArticleSage = plan.CodeArticleSage,
            Nom = plan.Nom,
            Designation = plan.Designation ?? string.Empty,
            Version = plan.Version,
            Statut = plan.Statut,
            DateApplication = plan.DateApplication?.ToDateTime(TimeOnly.MinValue),
            MachineDefautCode = plan.MachineDefautCode,
            LegendeMoyens = plan.LegendeMoyens ?? string.Empty,
            CreePar = plan.CreePar,
            CreeLe = plan.CreeLe,
            ModifiePar = plan.ModifiePar ?? string.Empty,
            ModifieLe = plan.ModifieLe,
            CommentaireVersion = plan.CommentaireVersion ?? string.Empty,
            Sections = plan.PlanFabSections?.Select(s => new PlanSectionResponseDto
            {
                Id = s.Id,
                ModeleSectionId = s.ModeleSectionId,
                OrdreAffiche = s.OrdreAffiche,
                LibelleSection = s.LibelleSection,
                FrequenceLibelle = s.FrequenceLibelle ?? string.Empty,
                Lignes = s.PlanFabLignes?.Select(l => new PlanLigneResponseDto
                {
                    Id = l.Id,
                    ModeleLigneSourceId = l.ModeleLigneSourceId,
                    OrdreAffiche = l.OrdreAffiche,
                    TypeCaracteristiqueId = l.TypeCaracteristiqueId ?? Guid.Empty,
                    LibelleAffiche = l.LibelleAffiche ?? string.Empty,
                    NomCategorie = l.TypeCaracteristique?.Libelle ?? string.Empty,
                    TypeControleId = l.TypeControleId ?? Guid.Empty,
                    MoyenControleId = l.MoyenControleId,
                    PeriodiciteId = l.PeriodiciteId,
                    InstrumentCode = l.InstrumentCode ?? l.MoyenTexteLibre ?? string.Empty,
                    ValeurNominale = l.ValeurNominale,
                    ToleranceSuperieure = l.ToleranceSuperieure,
                    ToleranceInferieure = l.ToleranceInferieure,
                    Unite = l.Unite ?? string.Empty,
                    LimiteSpecTexte = l.LimiteSpecTexte ?? string.Empty,
                    Observations = l.Observations ?? string.Empty,
                    Instruction = l.Instruction ?? string.Empty,
                    EstCritique = l.EstCritique
                }).ToList() ?? new List<PlanLigneResponseDto>()
            }).ToList() ?? new List<PlanSectionResponseDto>()
        };
    }

    public static PlanFabEntete ConstruireEntitePlanVierge(CreatePlanRequestDto dto, string designationSage)
    {
        return new PlanFabEntete
        {
            Id = Guid.NewGuid(),
            ModeleSourceId = null,
            CodeArticleSage = dto.CodeArticleSage,
            Designation = designationSage,
            OperationCode = dto.OperationCode,
            Nom = string.IsNullOrWhiteSpace(dto.Nom) ? $"PC-{dto.CodeArticleSage}" : dto.Nom,
            Version = 1,
            Statut = StatutsPlan.Brouillon,
            CreePar = "Admin",
            CreeLe = DateTime.UtcNow,
            CommentaireVersion = dto.CommentaireVersion,
            LegendeMoyens = string.IsNullOrWhiteSpace(dto.LegendeMoyens) ? null : dto.LegendeMoyens,
            PlanFabSections = new List<PlanFabSection>()
        };
    }
}